# Requirements — SupportDesk AI

## Intent Analysis

SupportDesk AI is a support-ticket management Web API built as a trainee assessment. The goal is not a large feature set but a **reliable, explainable MVP** that shows the trainee can apply ASP.NET Core concepts (controllers, DI, async, EF Core, DTOs, validation, error handling) and safely integrate an AI capability. The reviewer must be able to clone and run it from the README, and the trainee must be able to explain the controller -> service -> database/AI flow and make a small change live. [desc]

- **Request type**: new greenfield application, multi-component (API, persistence, AI integration, tests). [desc]
- **Complexity / depth**: Standard. Endpoints are well defined by the brief; the open decisions were settled in `requirements-analysis-questions.md`. [scope]
- **Primary actor**: a support agent using the API through Swagger (no frontend required). [desc]
- **Success**: all brief requirements (FR-01..FR-13) work, data persists across restarts, AI output is validated or rejected in a controlled way, an MCP client can list and call the eight SupportDesk tools, and the definition of done in the brief is met within 5 days. [desc]
- **Change Request 2026-10-21**: the trainer's updated brief adds an MCP server (FR-11 to FR-13, recorded here as FR13–FR15 with NFR11–NFR12). FR-01..FR-10 are unchanged, so the delivered units U1 and U2 stay valid. [desc-v2; MQ1–MQ3]

Working practices (branching, test-after with xUnit, 80% coverage floor, local checks in place of CI, `.editorconfig` + `dotnet format`, documented `dotnet ef database update`) come from the affirmed `team-practices.md` and are not repeated here except where they constrain a requirement. [memory:team-practices]

## Functional Requirements

Brief IDs (FR-01..FR-13) are kept in brackets for traceability to the source document. Our numbering differs from the brief's, so the mapping is:

| Brief | Ours | | Brief | Ours |
|---|---|---|---|---|
| FR-01 Create | FR2 | | FR-08 Dashboard | FR9 |
| FR-02 List | FR3 | | FR-09 Failure handling | FR10 |
| FR-03 Get | FR4 | | FR-10 Configuration | FR11 |
| FR-04 Update | FR5 | | FR-11 MCP server | FR13 |
| FR-05 Status | FR6 | | FR-12 MCP tools | FR14 |
| FR-06 AI analysis | FR7 | | FR-13 MCP safety and setup | FR15 |
| FR-07 Draft reply | FR8 | | §5 data model; §3 Swagger | FR1; FR12 |

### FR1 — Ticket data model [brief §5; Q2, Q3, Q4, Q6, Q7]
- **FR1.1** A Ticket has: `Id` (integer, database-generated), `Title`, `Description`, `Status`, `Priority`, `Category` (nullable), `Sentiment` (nullable), `AiSummary` (nullable), `SuggestedAction` (nullable), `CreatedAt` (UTC), `UpdatedAt` (UTC).
- **FR1.2** `Status` is an enum: `Open`, `InProgress`, `Resolved`, `Closed`. `Priority` is an enum: `Low`, `Medium`, `High`, `Critical`.
- **FR1.3** `Category` is an enum: `AccountAccess`, `Billing`, `TechnicalIssue`, `FeatureRequest`, `GeneralInquiry`, `Other` (displayed in JSON as `Account Access`, `Billing`, `Technical Issue`, `Feature Request`, `General Inquiry`, `Other`). `Sentiment` is an enum: `Positive`, `Neutral`, `Negative`, `Frustrated`. [Q3]
- **FR1.4** Enums are serialized as strings in requests and responses, never as numbers.
- **FR1.5** `CreatedAt` is set once on creation; `UpdatedAt` is set on creation and on every change (update, status change, analysis, draft).
- **FR1.6** The schema is created and evolved with EF Core migrations; data persists in a SQLite file across application restarts.

### FR2 — Create ticket [FR-01; Q6]
- **FR2.1** `POST /api/tickets` accepts `title` and `description` only.
- **FR2.2** A new ticket gets `Status = Open`, `Priority = Medium`, all AI fields null, and `CreatedAt = UpdatedAt = now (UTC)`.
- **FR2.3** On success the API returns `201 Created`, a `Location` header pointing to `/api/tickets/{id}`, and the created ticket.
- **FR2.4** Validation: `title` required, 3–200 characters after trimming; `description` required, 10–4000 characters after trimming. Whitespace-only values count as empty.
- **AC2.1** Given a valid title and description, When I POST, Then I get 201 with the ticket, `status = "Open"`, `priority = "Medium"`, and the ticket is still returned by GET after restarting the app.
- **AC2.2** Given a missing or whitespace-only title, When I POST, Then I get 400 naming the `title` field and nothing is saved.
- **AC2.3** Given a 201-character title or a 4001-character description, When I POST, Then I get 400 naming the field and its limit.

### FR3 — List tickets [FR-02; Q8]
- **FR3.1** `GET /api/tickets` returns all tickets, newest `CreatedAt` first, ties broken by `Id` descending.
- **FR3.2** Optional query filters `status`, `priority` and `category` may be combined (AND). Filter values are the enum names, matched case-insensitively.
- **FR3.3** ~~No pagination in this version.~~ **Changed 2026-10-22 at the person's request** (memory risk with thousands of tickets): page-based paging. `GET /api/tickets` takes `page` (≥ 1, default 1) and `pageSize` (1–200, default 50) and returns `{ items, page, pageSize, totalCount, totalPages }`; sorting stays newest first with id as tie-break so pages are repeatable; a page past the end is an empty list; out-of-range values return 400. The MCP `list_tickets` tool takes the same arguments with default 20 and maximum 50.
- **AC3.1** Given no tickets, When I GET, Then I get 200 with an empty array (not an error).
- **AC3.2** Given tickets of mixed status, When I GET `?status=Open&priority=High`, Then only tickets matching both are returned.
- **AC3.3** Given `?status=Pending` (not a valid value), When I GET, Then I get 400 naming the parameter and listing the allowed values.

### FR4 — Get ticket [FR-03]
- **FR4.1** `GET /api/tickets/{id}` returns the ticket including AI fields and the latest draft reply.
- **AC4.1** Given an existing id, Then 200 with the ticket. Given an unknown id, Then 404 with an error body naming the id.

### FR5 — Update ticket [FR-04; Q6]
- **FR5.1** `PUT /api/tickets/{id}` updates `title`, `description` and `priority` (all three required in the body, same validation as FR2.4; `priority` must be a valid enum name).
- **FR5.2** Status cannot be changed through PUT (only FR6); AI fields cannot be changed through PUT (only FR7/FR8).
- **FR5.3** Success returns 200 with the updated ticket and a refreshed `UpdatedAt`.
- **AC5.1** Given an unknown id, Then 404. Given an invalid priority such as `"Urgent"`, Then 400 and nothing changes.

### FR6 — Update status [FR-05; Q5]
- **FR6.1** `PATCH /api/tickets/{id}/status` with body `{ "status": "<value>" }` sets the status.
- **FR6.2** Any of the four statuses may be set from any status; no transition rules are enforced. Setting the current status again succeeds (idempotent).
- **AC6.1** Given `{ "status": "Resolved" }`, Then 200 with the updated ticket. Given `{ "status": "Done" }` or a missing status, Then 400 listing the allowed values. Given an unknown id, Then 404.

### FR7 — AI analysis [FR-06; Q2, Q3; NFR3]
- **FR7.1** `POST /api/tickets/{id}/analyze` sends the ticket's title and description to the configured AI provider and requests a structured result: `category`, `priority`, `sentiment`, `summary`, `suggestedAction`.
- **FR7.2** The AI result is parsed into a typed DTO and validated **before** anything is saved or returned:
  - `category`, `priority`, `sentiment` must each be one of the allowed values (FR1.2, FR1.3), matched case-insensitively and tolerant of spaces (e.g. `"Account Access"` -> `AccountAccess`);
  - `summary` required, 1–500 characters; `suggestedAction` required, 1–500 characters;
  - unknown extra fields are ignored; missing required fields or non-JSON output fail validation.
- **FR7.3** On valid output the ticket's `Category`, `Sentiment`, `AiSummary`, `SuggestedAction` are saved and `Priority` is **overwritten** with the AI's suggested priority; `UpdatedAt` is refreshed. Response: 200 with the analysis and the updated ticket. [Q2]
- **FR7.4** On invalid output nothing is saved and the API returns a controlled error (FR9).
- **FR7.5** Running analysis again replaces the previous AI fields.
- **AC7.1** Given the AI returns `priority: "High"` and valid other fields, Then the ticket's priority becomes High and the analysis fields are saved.
- **AC7.2** Given the AI returns `priority: "Urgent"` or malformed JSON, Then the ticket is unchanged and the API returns 502 with a message that the AI response was invalid.

### FR8 — Draft reply [FR-07; Q4]
- **FR8.1** `POST /api/tickets/{id}/draft-reply` asks the AI for a polite reply draft based on the ticket (and its analysis if present).
- **FR8.2** The draft is validated (non-empty, max 4000 characters) and returned with `ticketId`, `generatedAt` and `notice: "Draft — requires human review; not sent"`. **Changed 2026-10-22 at the person's request, to follow the brief (FR-07: "Return it for human review; do not send it")**: the draft is **not stored**; the `DraftReply` and `DraftReplyGeneratedAt` columns were removed by the `RemoveDraftReplyColumns` migration, and the ticket is not changed by this request. (Supersedes Q4 answer B.)
- **FR8.3** The system has **no** capability to send a reply (no email, webhook or messaging integration). [rule: never auto-send]
- **AC8.1** Given an existing ticket, Then 200 with the draft text and the review notice, and the ticket itself is unchanged (nothing is stored). Given the AI fails or returns empty text, Then no draft is saved and a controlled error is returned.

### FR9 — Dashboard summary [FR-08]
- **FR9.1** `GET /api/dashboard/summary` returns counts: `total`, `open` (Status = Open), `inProgress`, `resolved`, `closed`, `highPriority` (Priority = High or Critical, any status), and `unanalyzed` (Category is null).
- **AC9.1** Given no tickets, Then all counts are 0. Given 3 Open (one Critical) and 1 Resolved High ticket, Then `total = 4`, `open = 3`, `resolved = 1`, `highPriority = 2`.

### FR10 — Error responses [FR-09; NFR2]
- **FR10.1** All errors use one consistent JSON shape: RFC 7807 `ProblemDetails` (`type`, `title`, `status`, `detail`, plus `errors` per field for validation).
- **FR10.2** Status codes: 400 validation / invalid enum value; 404 unknown ticket; 502 AI provider error or invalid AI output; 504 AI timeout; 503 AI not configured (e.g. Groq selected but no key); 500 unexpected error.
- **FR10.3** Error bodies never contain stack traces, exception type names, connection strings, API keys or raw provider responses. Details are logged server-side only (without secrets).
- **AC10.1** Given an unhandled exception, Then the client receives a 500 ProblemDetails with a generic message and no stack trace.

### FR11 — AI provider and configuration [FR-10; FQ1]
- **FR11.1** AI access goes through an `IAiTicketService` interface with one production implementation: `GroqAiTicketService` (Groq's OpenAI-compatible chat completions API, `https://api.groq.com/openai/v1`). The interface stays so tests can use hand-written fakes and the provider can be replaced. [FQ1; changed 2026-10-20: no mock provider, per the person's decision in U2 functional design]
- **FR11.2** There is no mock provider and no provider switch; Groq is always used for the AI endpoints.
- **FR11.3** Groq settings: `Ai:Groq:ApiKey` (from `dotnet user-secrets` or environment variable only), `Ai:Groq:Model` (default `openai/gpt-oss-120b`), `Ai:Groq:BaseUrl`, `Ai:TimeoutSeconds`. Committed `appsettings.json` and `.env.example` hold placeholders only.
- **FR11.4** (withdrawn 2026-10-20 with the mock provider.)
- **AC11.1** Given no Groq API key is configured, Then AI endpoints return 503 "AI provider not configured" and all ticket CRUD endpoints still work.

### FR12 — API exploration [brief §3]
- **FR12.1** Swagger / OpenAPI UI is available in the Development environment and documents every endpoint, request and response shape, and enum values.

### FR13 — MCP server [FR-11; MQ1] (added 2026-10-21)
- **FR13.1** SupportDesk AI is exposed as a Model Context Protocol (MCP) server built with the official C# MCP SDK (`ModelContextProtocol` NuGet package), either as a separate project in the solution or as a startup mode of the API host (decided in design).
- **FR13.2** Transport is **stdio**: an MCP client starts the server as a local process (`--mcp`) and talks over standard input/output. [MQ1] **Changed 2026-10-21 at the person's request ("we always want to run both")**: the running web API also serves MCP over Streamable HTTP at `/mcp` (stateless, localhost, no authentication), with the same tools and services; stdio stays because the brief requires it.
- **FR13.3** The MCP server uses the same SQLite database, configuration (including `Ai:*` settings from user secrets or environment variables) and services as the REST API. The database path resolves to the same file whatever working directory the MCP client launches the server from. The MCP server does not create or migrate the schema (the documented `dotnet ef database update` step does, as for the API); if the database has not been created, tools return a controlled error. The API and the MCP server may run at the same time on the same file; SQLite's own locking applies and last write wins (A5).
- **FR13.4** Nothing but MCP protocol messages is written to standard output; logs go to standard error so they cannot corrupt the protocol stream.
- **AC13.3** Given the server running over stdio with logging at Information, When tools are called (including a failing AI call), Then every line on standard output is a valid MCP JSON-RPC message.
- **AC13.1** Given the documented start command, When an MCP client connects over stdio, Then the server completes the MCP handshake and lists exactly the eight tools of FR14.
- **AC13.2** Given no Groq key is configured, When the MCP server starts, Then it starts normally and every non-AI tool works.

### FR14 — MCP tools [FR-12; MQ3] (added 2026-10-21)
- **FR14.1** The server exposes exactly these tools, each with a clear name, a description and typed input parameters:

  | Tool | Input | Calls | Result |
  |---|---|---|---|
  | `create_ticket` | `title`, `description` | create (FR2) | the created ticket |
  | `list_tickets` | optional `status`, `priority`, `category` | list (FR3) | array of tickets |
  | `get_ticket` | `id` | get (FR4) | the ticket |
  | `update_ticket` | `id`, `title`, `description`, `priority` | update (FR5) | the updated ticket |
  | `update_ticket_status` | `id`, `status` | status change (FR6) | the updated ticket |
  | `analyze_ticket` | `id` | analysis (FR7) | the analysis, previous priority and updated ticket |
  | `draft_reply` | `id` | draft reply (FR8) | the draft with the "not sent" notice |
  | `get_dashboard_summary` | none | dashboard (FR9) | the counts |

- **FR14.2** Every tool calls the same service layer as the REST endpoint and contains no business logic of its own (no validation rules, no persistence, no AI calls duplicated in the tool).
- **FR14.3** Results are structured JSON with the same field names and enum spellings as the REST responses (`TicketResponse`, `AnalysisResponse` with `previousPriority`, `DraftReplyResponse`, dashboard summary), as defined in contract C1 of `inception/contract-design/contract-summary.md` and already returned by the REST API.
- **FR14.4** Failures are returned as **tool error results** (`isError: true`) with the same short, safe title and detail as the REST ProblemDetails (FR10), the matching HTTP-style `status` (400, 404, 502, 503, 504, 500) as a stable error kind, and per-field messages for validation failures: validation error, unknown ticket id, AI not configured, AI timeout, AI provider error, invalid AI output, unexpected error. They are not raised as protocol-level errors. [MQ3]
- **AC14.1** Given an MCP client, When it calls `create_ticket` with a valid title and description, Then the ticket is saved and returned, and `GET /api/tickets/{id}` returns the same ticket.
- **AC14.2** Given `update_ticket_status` with `status: "Done"`, Then the tool returns an error result listing the allowed values and nothing changes.
- **AC14.3** Given `get_ticket` with an unknown id, Then the tool returns an error result naming the id.
- **AC14.4** Given a Groq key, When the client calls `analyze_ticket` and then `draft_reply`, Then both return validated results that are saved exactly as through the REST endpoints; given an AI timeout or provider error, Then a controlled error result is returned and the ticket is unchanged.

### FR15 — MCP safety and client setup [FR-13; MQ2] (added 2026-10-21)
- **FR15.1** Tool arguments from the LLM client are untrusted input and are validated exactly as strictly as API requests (same length limits, enum names only — numbers such as `status: 1` and numeric strings such as `"1"` are rejected — and ids must be positive integers).
- **FR15.2** `draft_reply` returns a draft only and never sends anything (FR8.3 applies to MCP too).
- **FR15.3** No delete tool, no tool other than the eight in FR14.1, and no secrets, configuration values, stack traces, exception type names or raw provider responses in any tool result.
- **FR15.4** The README documents how to build and start the MCP server and how to connect it to an MCP client, with a sample client configuration for Claude Code (the demo client) and for Claude Desktop, and how to inspect it with MCP Inspector. [MQ2]
- **FR15.5** Evidence of at least one end-to-end tool call from Claude Code (screenshot or log), including `analyze_ticket` and `draft_reply`, is kept in the repository for submission. [MQ2]
- **AC15.1** Given Claude Code configured with the sample configuration, When the person asks it to list tickets and analyze one, Then Claude Code calls `list_tickets` and `analyze_ticket` successfully and the evidence is saved.
- **AC15.2** Given a tool result for any failure, Then it contains no stack trace, key or provider text.

## Non-Functional Requirements

- **NFR1 — Independence from AI**: create, list, get, update, status and dashboard endpoints never call the AI provider and work when it is down, slow or unconfigured. [rule: ALWAYS keep core operations working]
- **NFR2 — No leakage**: no secrets or stack traces in any HTTP response; secrets never logged; no secrets in the Git repository. [rules]
- **NFR3 — AI timeout**: every AI call has a configurable timeout (default 20 seconds) and honours the request `CancellationToken`; a timeout returns 504 within timeout + 2 seconds and leaves the ticket unchanged.
- **NFR4 — Untrusted AI output**: AI output is treated as untrusted data — never executed, never used as SQL, never allowed to set status. Ticket text sent to the AI is wrapped as data in the prompt, with instructions that the model returns JSON only. [rules]
- **NFR5 — Data protection**: only demo / synthetic ticket text is used with the real Groq provider and with MCP clients (an MCP client such as Claude Code is also an external LLM service that receives ticket text in tool results); the README states that real customer or personal data must not be entered. Ticket text returned to an MCP client is data, not instructions. [rule: never send personal data; updated 2026-10-21]
- **NFR6 — Performance (local)**: non-AI endpoints respond in under 500 ms for up to 1,000 tickets on a developer machine.
- **NFR7 — Async I/O**: all database and HTTP calls are async and pass a `CancellationToken`.
- **NFR8 — Maintainability**: thin controllers; business logic in services registered through DI; DTOs separate from EF entities; no extra architecture layers. [memory:team-practices]
- **NFR9 — Testability**: automated xUnit tests cover the key business rules and all AI-output validation paths (valid, malformed JSON, invalid enum, missing field, timeout, provider error); tests never call the real AI; at least 80% line coverage on hand-written code. MCP tests cover: exactly the eight tools are listed, each tool delegates to the service, failures become `isError` results without secrets or stack traces, and no delete tool exists. [memory:team-practices; MCP added 2026-10-21]
- **NFR10 — Reproducibility**: a reviewer can clone the repo and run it by following the README (prerequisites, setup, configuration, `dotnet ef database update`, endpoint examples, known limitations). Ticket CRUD, dashboard and all automated tests work with no API key; the two AI endpoints need the reviewer's own Groq key and return 503 without it (changed 2026-10-20: no mock provider).
- **NFR11 — MCP availability**: the MCP server starts from the documented steps, works with at least one MCP client (Claude Code), and its non-AI tools work when the AI provider is down, slow or unconfigured. AI tools use the same timeout (NFR3). [brief §8; added 2026-10-21]
- **NFR12 — MCP safety**: MCP tool arguments are untrusted input (validated like API requests), only the eight FR14 tools are exposed, and tool results never carry secrets or stack traces. The MCP server reuses the services, so NFR2, NFR4 and the hard rules apply unchanged. [brief §7, FR-13; added 2026-10-21]

## Constraints

- ASP.NET Core 8 Web API with controllers, C#, EF Core, SQLite, Swagger; .NET 8 SDK. [desc]
- MCP server with the official C# MCP SDK (`ModelContextProtocol` NuGet package), stdio transport. [desc-v2; MQ1]
- Local only — no cloud deployment, no CI service. [desc]
- 5-day delivery window; GitHub submission with meaningful commits is mandatory. [desc]
- AI provider: Groq (OpenAI-compatible API); the trainee supplies the API key locally. [FQ1]
- Hard rules affirmed in Practices Discovery (`aidlc/spaces/default/memory/project.md`): never commit secrets; never auto-send AI replies; never save/return unvalidated AI output; never send real personal data to an external AI; never execute AI-generated content; always keep core operations working without the AI.

## Assumptions

- **A1** Single user, no authentication or authorization (brief marks full auth out of scope); anyone who can reach the local API can use every endpoint. [desc]
- **A2** Length limits (title 3–200, description 10–4000, summary/action 1–500, draft 1–4000) are reasonable defaults; the brief only says "reasonable length limits". [assumption]
- **A3** "High-priority" on the dashboard means High **or** Critical; "open" means Status = Open only (InProgress is reported separately). [assumption]
- **A4** No delete endpoint: tickets are closed via status, matching the brief's "create, view, update, filter, and close". [desc]
- **A5** Concurrent edits are last-write-wins; a single local user makes conflicts unlikely. [assumption]
- **A6** The AI may return enum values in display form or with different casing; tolerant matching to the allowed list is acceptable, anything outside the list is rejected. [assumption]
- **A7** No automatic retry of failed AI calls; the user can call the endpoint again (avoids repeating side effects). [assumption]
- **A8** The Groq model name is configuration, not code; a default model is documented in the README. [assumption]

## Out of Scope

- Sending replies (email, chat, webhooks) — drafts only.
- Authentication/authorization, multi-tenancy, user accounts.
- Delete endpoint, pagination, sorting options, full-text search.
- Frontend UI (Swagger is the interface).
- RAG, vector databases, fine-tuning, multi-agent workflows.
- Production deployment, monitoring, scaling, CI pipelines.
- Ticket history/audit log and comments.
- MCP authentication (OAuth), remote hosting of the MCP server, multi-user MCP sessions, and streamable HTTP transport. [desc-v2; MQ1]
- MCP resources and prompts (only the eight tools are exposed).

## Open Questions

- None blocking. The exact default Groq model name is chosen during Code Generation and documented in the README (A8).

## Sources

- `[desc]` Initial description: "Build SupportDesk AI, an ASP.NET Core 8 Web API with EF Core + SQLite, following the catalogued requirements document … Local only, no cloud deployment." and the catalogued brief `documents/supportdesk-requirements.pdf` (FR-01..FR-10, §3, §5, §7–§11).
- `[desc-v2]` The trainer's updated brief (2026-10-21), saved as `docs/requirements.pdf` and re-catalogued as `documents/supportdesk-requirements.pdf`: adds FR-11 MCP server, FR-12 MCP tools, FR-13 MCP safety and client setup, and the matching §7, §8, §9, §10, §11 and §12 items. FR-01..FR-10 are unchanged.
- `[scope]` Workflow-selected scope: classic, Standard depth.
- `[Q1]`–`[Q8]`, `[FQ1]`, `[MQ1]`–`[MQ3]` — answers in `requirements-analysis-questions.md`.
- `[memory:team-practices]` — `inception/practices-discovery/team-practices.md` and the affirmed rules in `aidlc/spaces/default/memory/project.md`.
- `[assumption]` — defaults chosen by the product manager, listed under Assumptions for correction at approval.
