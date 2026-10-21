# Architecture Decisions — SupportDesk AI

Decisions taken in Domain Design from the answers in `domain-design-questions.md`, grounded in `requirements.md`, `stories.md` and the affirmed practices and rules (`team-practices.md`, `aidlc/spaces/default/memory/project.md`). Component names refer to `components.md`.

## ADR-001: Three building blocks — Tickets, AiAssistant, Dashboard

### Status
Accepted (2026-10-19)

### Context
The brief asks for a small, explainable API; the trainee must explain the controller -> service -> database/AI flow and make a live change. Team practice forbids architecture layers the brief does not need. The analyze and draft flows are short (load ticket, call AI, save result) but must keep untrusted AI output away from ticket data.

### Decision
Split the code into three building blocks: **Tickets** (owns Ticket, all CRUD/status rules, and the analyze/draft flows), **AiAssistant** (everything about talking to and validating the AI) and **Dashboard** (read-only counts). Error handling, controllers, Swagger and configuration are shared plumbing, not blocks.

### Consequences
- Positive: each block maps to one story group, which makes the walkthrough simple; ticket rules live in one place; the AI can be replaced by a fake in tests.
- Negative: the Tickets service holds both CRUD and the two AI flows, so it is the largest class; if AI flows grow (retries, history) it may need splitting.
- Security: only Tickets writes ticket data, and it only accepts validated AI results (see ADR-002), which supports the "never save unvalidated AI output" rule.

### Alternatives Rejected
- **Four blocks with a TicketAiWorkflow orchestrator**: keeps Tickets pure CRUD but adds a layer for a two-step flow and splits ticket-changing logic across two places.
- **Two blocks (Dashboard inside Tickets)**: fewer files, but mixes reporting queries with lifecycle rules and makes Tickets larger.

## ADR-002: AiAssistant validates all AI output before anyone else sees it

### Status
Accepted (2026-10-19). Amended 2026-10-20: the Mock provider was dropped (Groq only, decided in U2 Functional Design); the boundary and the shared validator are unchanged, and tests use hand-written fakes of `IAiTicketService`. References to Mock below are historical.

### Context
Hard rules: never save or return AI output that has not been validated against a typed DTO and allowed values; never execute AI output; never send personal data to an external AI; always time out AI calls. Two providers (Mock and Groq) must behave identically from the caller's point of view.

### Decision
AiAssistant is an anti-corruption boundary. Its `IAiTicketService` returns only a validated typed result or a typed failure (invalid output, timeout, provider error, not configured). One shared validator, used by both Mock and Groq, checks JSON shape, allowed values (case- and space-tolerant), and lengths. Prompts wrap ticket text as data and request JSON only. The provider is chosen by configuration, defaulting to Mock.

### Consequences
- Positive: Tickets can trust what it receives; validation is tested once and applies to both providers; swapping provider is configuration only.
- Negative: AiAssistant must know the allowed Category/Sentiment/Priority values, so it shares those enums with Tickets.
- Security: raw provider responses and the API key never leave AiAssistant; untrusted output is never executed or interpreted as commands.

### Alternatives Rejected
- **Validate inside Tickets**: AiAssistant would return raw parsed JSON, so unvalidated data would cross into the ticket-owning code, and each caller would have to remember to validate.

## ADR-003: Dashboard uses read-only queries over ticket data

### Status
Accepted (2026-10-19)

### Context
The dashboard needs seven counts. Tickets owns the Ticket entity. Counting is a read-only concern with no business rules about changing tickets.

### Decision
Dashboard runs read-only count queries directly over the Ticket table through EF Core. It never writes. Tickets remains the only writer.

### Consequences
- Positive: counts are computed in the database in one place; Tickets stays free of reporting code; the dashboard works when the AI is down.
- Negative: Dashboard is coupled to the Ticket table's shape; a schema change must update its queries.
- Security: no write path, so it cannot corrupt ticket data.

### Alternatives Rejected
- **Call a counting method on the Tickets service**: fully encapsulated, but adds reporting methods to Tickets and an extra hop for a simple read.

## ADR-004: Services use the EF Core DbContext directly (no repository layer)

### Status
Accepted (2026-10-19)

### Context
Team practice: business logic in services via DI, no unnecessary layers. EF Core's DbContext already provides a repository and unit-of-work. Tests use EF Core with SQLite in-memory.

### Decision
Services take the application DbContext through dependency injection and query/save with it directly. No `ITicketRepository` interface.

### Consequences
- Positive: less code to write and explain; tests exercise real EF behaviour against SQLite in-memory.
- Negative: services are tied to EF Core; replacing the data access technology would touch service code.
- Security: parameterised EF queries avoid SQL injection; no raw SQL is built from input.

### Alternatives Rejected
- **Repository interface between services and EF**: easier to mock, but duplicates what EF already provides and adds a layer the brief does not need.

## ADR-005: Typed exceptions plus one global handler produce all error responses

### Status
Accepted (2026-10-19)

### Context
FR10 requires one consistent ProblemDetails shape and specific codes (400, 404, 502, 503, 504, 500) with no stack traces or secrets. Controllers must stay thin.

### Decision
Services throw a small set of meaningful exceptions: ticket not found, AI timeout, invalid AI output, AI provider error, AI not configured. One global exception handler maps them to ProblemDetails (404, 504, 502, 502, 503) and anything unexpected to a generic 500. Model validation failures use the framework's standard 400 ProblemDetails.

### Consequences
- Positive: controllers stay one-liners; error mapping is in one place and easy to test.
- Negative: control flow uses exceptions for expected failures, which some consider less explicit than result types.
- Security: the handler is the single place that guarantees no stack traces, exception types or secrets reach the client.

### Alternatives Rejected
- **Result objects mapped in each controller**: explicit, but repeats mapping logic in every controller and makes controllers thicker.

## ADR-006: MCP server runs as a startup mode of the API project and calls the same services

### Status
Accepted (2026-10-21, MCP change request; answers MQ4 and MFQ1 in `domain-design-questions.md`)

### Context
The trainer's updated brief (FR-11 to FR-13; our FR13–FR15) requires an MCP server built with the official C# MCP SDK, stdio transport, exposing eight tools that "call the same service layer as the REST endpoints and must not duplicate business logic". The brief allows a separate project or a startup mode of the API host. The person wants the MCP code kept in the same project as the API, with each tool calling the same service methods the controllers call. Units U1 and U2 are already built and merged, so the change must not disturb them.

### Decision
- The MCP server lives in `src/SupportDesk.Api` (an `Mcp/` folder) and starts when the API is launched with `--mcp`: the program then builds a generic host with `AddMcpServer().WithStdioServerTransport()` instead of the web host.
- The ticket and dashboard services, the database context, the clock and the AI client are registered by one shared registration method used by both hosts, so the REST controllers and the MCP tools resolve the same implementations.
- Each tool calls the same service method as its REST endpoint (`ITicketService` for seven tools, the Dashboard service for `get_dashboard_summary`). There is no HTTP hop: the web API does not need to be running.
- Typed failures are mapped to tool error results by one MCP error mapper that reuses the REST handler's titles, details and status codes, so both entry points report failures the same way.
- **Launch**: the client starts the already built output (`dotnet <repo>/src/SupportDesk.Api/bin/Debug/net8.0/SupportDesk.Api.dll --mcp`), not `dotnet run`, so build messages can never reach standard output. The README says to run `dotnet build` first.
- **Logging**: the MCP host clears the default logging providers and adds only the console logger with `LogToStandardErrorThreshold = LogLevel.Trace`, so every log line goes to standard error and standard output carries MCP messages only.
- **Database path**: the MCP host sets its content root to the API project folder, found by walking up from `AppContext.BaseDirectory` (the `bin/...` folder) to the folder that contains `SupportDesk.Api.csproj`. `DatabasePath` then resolves `App_Data/supportdesk.db` exactly as it does for the web API, whatever working directory the client launches from. If no project folder is found (for example a published copy), an explicit absolute `ConnectionStrings__SupportDesk` environment variable in the client configuration is used instead, and the README shows it.
- **Configuration and the Groq key**: an MCP client launches the process without `ASPNETCORE_ENVIRONMENT`, so it runs as Production, where user secrets are not loaded by default. The MCP host therefore adds user secrets explicitly (`AddUserSecrets<Program>(optional: true)`) after `appsettings.json` and before environment variables. The key set once with `dotnet user-secrets` works for both hosts, and an `Ai__Groq__ApiKey` environment variable in the client configuration still overrides it.
- **DI scope**: the MCP SDK creates a service scope for each tool call. Tools receive `ITicketService` and the dashboard service as method parameters resolved from that scope, so each call gets its own `DbContext`, just like an HTTP request.
- **Shared registration**: the existing service registrations in `Program.cs` (DbContext, clock, ticket service, AI options and HTTP client, dashboard service) move into one extension method used by both hosts. The web host keeps controllers, Swagger and the exception handler. The existing `WebApplicationFactory` tests keep working because the web host registers the same services as before.
- **Shared database file**: the web API and an MCP process may run at the same time on the same SQLite file. SQLite file locking serialises writes, and concurrent edits are last-write-wins (A5). A write blocked by a lock longer than SQLite's busy timeout becomes the generic 500 error on either side. Neither host creates or migrates the schema (`dotnet ef database update` does).

### Consequences
- Positive: one project, one set of services and one configuration (the same user-secrets ID, loaded explicitly by the MCP host, so the Groq key set for the API works for MCP); the "no duplicated business logic" rule holds by construction; easy to show in the walkthrough ("the controller and the tool call the same method").
- Negative: `Program.cs` branches between two hosts, and the API project now references the MCP SDK even when running as a web server; the web-only setup (controllers, Swagger, exception handler) must stay out of the MCP host.
- Security: stdio only, so no new network listener; tool arguments go through the same validation as API requests; the error mapper is the one place guaranteeing that no stack traces, exception types or secrets reach the MCP client. Loading user secrets outside Development is acceptable because the MCP server is a local, single-user process; secrets still live only in the user profile, never in the repository. Logs on standard error are seen by the MCP client, so the existing rule that logs never contain the key, ticket text or AI text matters here too.

### Alternatives Rejected
- **Separate console project `SupportDesk.Mcp` referencing the API project**: clean separation, but the person wants the MCP code in the same project as the API, and it needs a second user-secrets setup.
- **Shared class library `SupportDesk.Core` used by both**: the cleanest split, but it moves about 30 already-merged files and adds a layer the brief does not need.
- **Tools calling the REST endpoints over HTTP**: requires the web API to be running at the same time and translating ProblemDetails back; further from the brief's "same service layer" wording (MFQ1 answer A).
