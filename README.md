# SupportDesk AI

A support-ticket management API built with ASP.NET Core 8, EF Core and SQLite. Agents can create, view, update,
filter and close tickets. An AI service (Groq) suggests a ticket's category, priority, sentiment, summary and next
action, and drafts a reply for a human to review. **AI replies are never sent automatically.** The same features
are available to LLM clients through a Model Context Protocol (MCP) server.

> **Use demo data only.** Ticket text is sent to Groq for analysis and drafts, and to your MCP client (for example
> Claude Code) in tool results. Never enter real customer, confidential or personal data.

## Contents

- [Prerequisites](#prerequisites)
- [Setup](#setup)
- [Configuration (Groq key)](#configuration-groq-key)
- [Run the API](#run-the-api)
- [Endpoints and examples](#endpoints-and-examples)
- [MCP server](#mcp-server)
- [Tests](#tests)
- [Project structure](#project-structure)
- [Design notes](#design-notes)
- [Known limitations](#known-limitations)

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (8.0.1xx or later)
- Git
- Optional, for the AI features: a free Groq API key from <https://console.groq.com/keys>
- Optional, for MCP: an MCP client such as Claude Code, Claude Desktop or MCP Inspector (needs Node.js for `npx`)

## Setup

Run these from the repository root:

```bash
git clone <repository-url> support-desk
cd support-desk

dotnet restore
dotnet tool restore                                   # installs the pinned dotnet-ef tool (.config/dotnet-tools.json)
dotnet ef database update --project src/SupportDesk.Api
dotnet build
```

`dotnet ef database update` creates the SQLite database at `src/SupportDesk.Api/App_Data/supportdesk.db` from
the EF Core migrations. The app never creates or migrates the database by itself, and the database file is not
committed. To start again with an empty database, delete that file and run the command again.

## Configuration (Groq key)

Ticket operations and the dashboard need **no configuration**. Only the two AI features
(`analyze` and `draft-reply`) need a Groq API key. Without one they return `503 AI features are not configured`
and everything else keeps working.

Store the key with .NET user secrets. They are kept in your user profile, outside the repository:

```bash
dotnet user-secrets set "Ai:Groq:ApiKey" "<your-groq-api-key>" --project src/SupportDesk.Api
```

Or use an environment variable (see [`.env.example`](.env.example) for all names):

```bash
export Ai__Groq__ApiKey="<your-groq-api-key>"
```

| Setting | Default | Notes |
|---|---|---|
| `Ai:Groq:ApiKey` | empty | User secrets or environment only. Never put a real key in `appsettings.json`. |
| `Ai:Groq:Model` | `openai/gpt-oss-120b` | Any chat model your Groq key can use |
| `Ai:Groq:BaseUrl` | `https://api.groq.com/openai/v1` | Must be `https` |
| `Ai:TimeoutSeconds` | `20` | 1–120. A slower AI call returns 504 and nothing is saved |
| `Ai:MaxRequestsPerMinute` | `10` | 1–1000. AI requests (analyze and draft, REST and MCP together) allowed per minute; more get 429 with `Retry-After` |
| `ConnectionStrings:SupportDesk` | `Data Source=App_Data/supportdesk.db` | Relative to the API project folder |

## Run the API

```bash
dotnet run --project src/SupportDesk.Api --launch-profile http
```

- API: <http://localhost:5189>
- Swagger UI: <http://localhost:5189/swagger> (Development environment, which the `http` profile sets)

## Endpoints and examples

| Method | Path | Purpose | Success |
|---|---|---|---|
| POST | `/api/tickets` | Create a ticket (title, description) | 201 + `Location` |
| GET | `/api/tickets?status=&priority=&category=&page=&pageSize=` | List tickets, newest first, one page at a time (default 50, max 200); optional filters | 200 |
| GET | `/api/tickets/{id}` | Get one ticket | 200 |
| PUT | `/api/tickets/{id}` | Update title, description and priority | 200 |
| PATCH | `/api/tickets/{id}/status` | Set status: `Open`, `InProgress`, `Resolved`, `Closed` | 200 |
| POST | `/api/tickets/{id}/analyze` | AI analysis: validated, saved, overwrites priority | 200 |
| POST | `/api/tickets/{id}/draft-reply` | AI reply draft, returned for human review; not saved, never sent | 200 |
| GET | `/api/dashboard/summary` | Ticket counts | 200 |

Ready-made requests for every endpoint, including invalid ones, are in
[`src/SupportDesk.Api/SupportDesk.Api.http`](src/SupportDesk.Api/SupportDesk.Api.http). You can run them with
the VS Code REST Client, Visual Studio or a JetBrains IDE, or copy them into Postman.

**Create a ticket**

```http
POST /api/tickets
Content-Type: application/json

{ "title": "Unable to access my account", "description": "I reset my password but still cannot sign in." }
```

```json
{
  "id": 4, "title": "Unable to access my account",
  "description": "I reset my password but still cannot sign in.",
  "status": "Open", "priority": "Medium",
  "category": null, "sentiment": null, "aiSummary": null, "suggestedAction": null,
  "createdAt": "2026-10-21T13:11:29.2817486Z", "updatedAt": "2026-10-21T13:11:29.2817486Z"
}
```

**List tickets** (`GET /api/tickets?status=Open&page=2&pageSize=50`): results come one page at a time, so a
response is never larger than 200 tickets however many exist. A page past the end returns an empty `items` list,
and `page < 1` or `pageSize` outside 1–200 returns 400.

```json
{ "items": [ { "id": 950, "title": "...", "status": "Open", "...": "..." } ],
  "page": 2, "pageSize": 50, "totalCount": 1000, "totalPages": 20 }
```

**Analyze a ticket** (`POST /api/tickets/1/analyze`): the AI output is checked against the allowed values and
lengths before anything is saved.

```json
{
  "category": "AccountAccess", "priority": "High", "sentiment": "Frustrated",
  "summary": "The customer cannot sign in after resetting their password.",
  "suggestedAction": "Check the account status and the password reset flow.",
  "previousPriority": "Medium",
  "ticket": { "id": 1, "...": "the updated ticket" }
}
```

**Draft a reply** (`POST /api/tickets/1/draft-reply`). The draft is returned for an agent to review and edit.
It is **not saved** on the ticket and **never sent** (brief FR-07):

```json
{
  "ticketId": 1,
  "draft": "Hello,\n\nThank you for reaching out ...",
  "generatedAt": "2026-10-21T13:12:01.5Z",
  "notice": "Draft — requires human review; not sent"
}
```

**Dashboard** (`GET /api/dashboard/summary`). `open` counts only status Open, `highPriority` counts High and
Critical tickets in any status, and `unanalyzed` counts tickets with no category yet:

```json
{ "total": 4, "open": 3, "inProgress": 0, "resolved": 1, "closed": 0, "highPriority": 1, "unanalyzed": 2 }
```

**Errors** are always RFC 7807 ProblemDetails, with a `traceId` and no stack traces or secrets:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.", "status": 400,
  "detail": "See the errors property for the fields that failed validation.",
  "errors": { "status": ["Status is not valid. Allowed values: Open, InProgress, Resolved, Closed."] },
  "traceId": "00-93800d0eccb67722bdc76e0ddc2f8774-893b06077de550d8-00"
}
```

| Status | When |
|---|---|
| 400 | Missing or invalid field, unknown enum value, malformed JSON, non-numeric or non-positive id |
| 404 | Unknown ticket id or route |
| 502 | The AI returned invalid output (nothing saved), or the AI provider failed |
| 429 | Too many AI requests this minute (`Ai:MaxRequestsPerMinute`); see the `Retry-After` header. Other endpoints are not limited |
| 503 | AI features are not configured (no Groq key) |
| 504 | The AI provider timed out (nothing saved) |
| 500 | Unexpected error (generic message; details only in the server log) |

## MCP server

MCP is available in two ways. Both use the same eight tools, services, database and configuration as the REST
API:

- **HTTP, always on with the API:** while the API runs, MCP is served at `http://localhost:5189/mcp`
  (Streamable HTTP). Start the API once and connect the client by URL.
- **stdio:** the client starts its own copy of the program with `--mcp` and talks over standard input and
  output. The API does not need to be running, and nothing listens on the network. The brief requires this
  transport.

| Tool | Same as | Input |
|---|---|---|
| `create_ticket` | `POST /api/tickets` | `title`, `description` |
| `list_tickets` | `GET /api/tickets` | optional `status`, `priority`, `category`, `page`, `pageSize` (default 20, max 50) |
| `get_ticket` | `GET /api/tickets/{id}` | `id` |
| `update_ticket` | `PUT /api/tickets/{id}` | `id`, `title`, `description`, `priority` |
| `update_ticket_status` | `PATCH /api/tickets/{id}/status` | `id`, `status` |
| `analyze_ticket` | `POST /api/tickets/{id}/analyze` | `id` |
| `draft_reply` | `POST /api/tickets/{id}/draft-reply` (never sends) | `id` |
| `get_dashboard_summary` | `GET /api/dashboard/summary` | — |

Results use the same JSON as the REST API. Failures come back as tool error results (`isError: true`) with the
same `status`, `title` and `detail` as the REST errors. There is no delete tool.

**Option A: HTTP (easiest).** Start the API (`dotnet run --project src/SupportDesk.Api --launch-profile http`),
then:

```bash
claude mcp add --transport http supportdesk http://localhost:5189/mcp
```

The API must be running while you use the tools. MCP Inspector can also connect to the same URL, using the
"Streamable HTTP" transport.

**Option B: stdio.**

**1. Build once**, after the [setup](#setup) steps. The client starts the built program, not `dotnet run`, so
build output can never mix with MCP messages:

```bash
dotnet build
```

The program path below is `<repo>/src/SupportDesk.Api/bin/Debug/net8.0/SupportDesk.Api.dll`. Replace `<repo>`
with the absolute path of your clone. The Groq key from `dotnet user-secrets` is picked up automatically, and the
database is the same file the API uses, whatever folder the client starts from.

**2a. Claude Code** (the client used for the demo):

```bash
claude mcp add supportdesk -- dotnet <repo>/src/SupportDesk.Api/bin/Debug/net8.0/SupportDesk.Api.dll --mcp
```

Restart Claude Code and run `/mcp`. `supportdesk` should be connected with eight tools. Then try: "list my open
tickets", "analyze ticket 1", "draft a reply for ticket 1".

**2b. Claude Desktop** (`claude_desktop_config.json`), or a project `.mcp.json` for Claude Code:

```json
{
  "mcpServers": {
    "supportdesk": {
      "command": "dotnet",
      "args": ["<repo>/src/SupportDesk.Api/bin/Debug/net8.0/SupportDesk.Api.dll", "--mcp"]
    }
  }
}
```

To pass the key through the client instead of user secrets, add `"env": { "Ai__Groq__ApiKey": "<your-key>" }`.
Only do this in a local, uncommitted config file.

**2c. MCP Inspector** (browser-based test client):

```bash
npx @modelcontextprotocol/inspector dotnet <repo>/src/SupportDesk.Api/bin/Debug/net8.0/SupportDesk.Api.dll --mcp
```

Evidence of real tool calls is in [`docs/mcp-evidence/`](docs/mcp-evidence/).

## Tests

```bash
dotnet test
```

207 tests use xUnit, SQLite in-memory databases and hand-written fakes for the AI. They never call Groq and need
no key or network. They cover validation, the ticket, AI and dashboard services, the Groq client (with a stub
network), error mapping, the MCP tools, the HTTP API end to end (including AI failures, rate limiting and
parallel requests) and MCP over HTTP. Line coverage is about 97%.

Run one area at a time with a filter, for example `dotnet test --filter "Unit=U2"` for the AI tests
(`U1` tickets, `U2` AI, `U3` dashboard, `U5` MCP). For line coverage (EF migrations and `Program.cs` excluded):

```bash
dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"
```

## Project structure

```
src/SupportDesk.Api/
  Features/              one folder per feature (controller + service + DTOs together)
    Tickets/             TicketsController, ITicketService/TicketService (business rules), plus:
      Dtos/              API request/response shapes (TicketResponse, PagedResponse, AnalysisResponse, ...)
      Models/            service inputs/outputs (CreateTicketInput, TicketListFilter, PageRequest, PagedResult, ...)
      Validation/        FluentValidation validators (create, update, status, filters, paging) + TicketInputParsing
    Ai/                  IAiTicketService (contract), AiPrompts, the AI request limiter, plus:
      Models/            AiAnalysisResult, AiDraftResult, AiDraftContext (validated results)
      Configuration/     AiOptions, GroqOptions (bound from the Ai settings)
      Validation/        AiOutputValidator (untrusted AI text -> typed result or 502)
      Groq/              GroqAiTicketService (timeout, error mapping); Dtos/ = Groq request/response shapes
    Dashboard/           DashboardController, IDashboardService/DashboardService (read-only counts), plus:
      Dtos/              DashboardSummaryResponse;  Models/ DashboardSummary
  Mcp/                   MCP entry point: the eight tools, stdio host (--mcp), shared MCP setup (HTTP at /mcp)
  Domain/                Ticket entity and the Status, Priority, Category, Sentiment enums (shared)
  Errors/                Typed exceptions, the shared error mapping and the global exception handler
  Infrastructure/        DbContext, migrations, shared service registration, JSON settings, clock, DB path
  Program.cs             composition root: web host (REST + MCP over HTTP) or --mcp stdio host
tests/SupportDesk.Api.Tests/   xUnit tests
docs/                          brief (requirements.pdf), progress log, MCP evidence
aidlc/                         AI-DLC planning and design records (requirements, stories, ADRs, plans, reviews)
```

Request flow: **controller or MCP tool → service → database / AI**. Controllers and tools are thin and call the
same service methods. Business rules live in the services, and the AI adapter returns only validated, typed
results.

## Design notes

- **Validation with FluentValidation.** Each service input has a validator (`Features/Tickets/Validation/`), for
  example `CreateTicketInputValidator`. The service runs it before doing anything, so REST and MCP get identical
  checks, and every failing field is reported in one 400 response.
- **AI output is untrusted.** It is parsed into a typed result and checked against the allowed enum values and
  length limits. Anything else is rejected with 502 and nothing is saved. AI text is never executed or used in SQL.
- **Ticket text is sent as data.** The prompt sends title and description as an escaped JSON object, so ticket
  text cannot change the instructions. The ticket id and timestamps are never sent.
- **The AI is optional.** Every non-AI endpoint and tool works when Groq is down, slow or not configured. AI calls
  have a timeout and no automatic retries.
- **The AI quota is protected.** A shared limit (default 10 AI requests per minute) applies to REST and MCP
  together, so a script or a looping AI client cannot use up the Groq quota. Unknown ticket ids are rejected
  before they count.
- **No secrets in Git.** `appsettings.json` and `.env.example` hold placeholders only. Keys and ticket or AI text
  are never logged.
- Decisions and their trade-offs (ADR-001 to ADR-006) are in
  `aidlc/spaces/default/intents/261019-supportdesk-ai/inception/domain-design/decisions.md`.

## Known limitations

- No authentication or authorization. The API is meant for a single local user.
- Paging is page-based (`page`/`pageSize`): if tickets are added while you page through, an item can appear on two
  pages. There are no sorting options or full-text search. Tickets cannot be deleted; close them instead.
- Concurrent edits are last-write-wins. The API and the MCP server may share the SQLite file at the same time.
- Re-running analysis overwrites the ticket's priority and earlier analysis. No history is kept.
- The AI features need your own Groq key. Groq can retire models, so set `Ai:Groq:Model` if the default
  disappears (a retired model shows up as 502).
- MCP has no authentication on either transport. The HTTP endpoint is meant for `localhost` only, and the server
  exposes tools only (no resources or prompts).
- The Swagger document lists `text/plain` and `text/json` as extra success media types, and its schema names
  differ slightly from the design contract (for example `TicketStatus` instead of `Status`). The JSON itself
  matches.
