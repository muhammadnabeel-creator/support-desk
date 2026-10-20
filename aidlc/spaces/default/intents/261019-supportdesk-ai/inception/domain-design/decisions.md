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
Accepted (2026-10-19)

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
