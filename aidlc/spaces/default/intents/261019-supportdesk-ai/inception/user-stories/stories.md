# User Stories — SupportDesk AI

Source: `inception/requirements-analysis/requirements.md` (FR1–FR15, NFR1–NFR12; FR13–FR15 and NFR11–NFR12 added by the MCP change request of 2026-10-21), personas in `personas.md`, plan answers in `user-stories-questions.md`. Testing posture (test-after, xUnit, fakes for the AI) from `inception/practices-discovery/team-practices.md`: every acceptance criterion below is written so it can become an automated test that never calls a real AI.

Priorities use MoSCoW. The MVP line is decided in Delivery Planning.

## Story Map

| Group | Must Have | Should Have | Won't Have (this time) |
|---|---|---|---|
| 1 Ticket Management | US1.1 Create, US1.2 List & filter, US1.3 View, US1.4 Edit, US1.5 Change status | — | Delete a ticket; pagination; sorting options; full-text search |
| 2 AI Assistance | US2.1 Analyze, US2.2 Reject bad AI output, US2.3 Survive AI outages, US2.4 Draft reply, US2.5 Connect Groq | — | Sending replies (email, chat, webhooks); automatic AI retries |
| 3 Dashboard | US3.1 Summary counts | — | Charts or trends over time |
| 4 Reliability & Setup | US4.1 Consistent safe errors, US4.2 Run from a clean clone (release gate), US4.4 Secrets stay out of Git | US4.3 Example requests | Authentication; frontend UI; CI pipeline |
| 5 MCP Access (added 2026-10-21) | US5.1 Connect an MCP client, US5.2 Manage tickets through tools, US5.3 AI through tools, US5.4 Safe tool use, US5.5 MCP setup and evidence | — | MCP over HTTP; MCP authentication; remote hosting; delete tool; MCP resources and prompts |

No story is Could Have: the brief's scope is small enough that everything in it is either needed (Must/Should) or explicitly excluded (Won't).

## 1. Ticket Management

### US1.1 — Create a ticket
**As** Sam, **I want** to log a ticket with a title and description, **so that** a customer's problem is recorded and can be worked on.
- **Priority**: Must Have
- **Traces**: FR2, FR1
- **AC1.1.1** Given a title of 3–200 characters and a description of 10–4000 characters, When Sam POSTs to `/api/tickets`, Then the response is 201 with a `Location` header and the ticket, with `status` "Open", `priority` "Medium" and empty AI fields.
- **AC1.1.2** Given a missing or whitespace-only title, When Sam POSTs, Then the response is 400 naming `title`, and no ticket is saved.
- **AC1.1.3** Given a 201-character title or a 4001-character description, When Sam POSTs, Then the response is 400 naming the field and its limit.
- **AC1.1.4** Given Sam created a ticket, When the application is restarted and Sam GETs it by id, Then the same ticket is returned.
- **AC1.1.5** Given a title of "  Login broken  " (spaces around it), When Sam POSTs, Then the ticket is saved with the title "Login broken", and the length limits are checked against the trimmed text (so a title of two letters padded with spaces is rejected with 400).
- **Dependencies**: none. **INVEST**: independent first slice; small; testable through the API.

### US1.2 — List and filter tickets
**As** Sam, **I want** to list tickets and narrow them by status, priority or category, **so that** I can see what needs my attention.
- **Priority**: Must Have
- **Traces**: FR3
- **AC1.2.1** Given no tickets exist, When Sam GETs `/api/tickets`, Then the response is 200 with an empty array.
- **AC1.2.2** Given several tickets, When Sam GETs `/api/tickets`, Then they are returned newest `createdAt` first, and tickets with the same `createdAt` are ordered by highest id first.
- **AC1.2.3** Given tickets with mixed status and priority, When Sam GETs `?status=Open&priority=High`, Then only tickets matching both filters are returned.
- **AC1.2.4** Given `?status=Pending`, When Sam GETs, Then the response is 400 naming the parameter and listing the allowed values.
- **AC1.2.5** Given tickets analyzed as Billing and as Technical Issue, When Sam GETs `?category=Billing`, Then only the Billing tickets are returned.
- **AC1.2.6** Given an Open ticket, When Sam GETs `?status=open` (lower case), Then the ticket is returned, because filter values are matched case-insensitively.
- **Dependencies**: US1.1 for data (tests seed data directly). **INVEST**: small; testable.

### US1.3 — View one ticket
**As** Sam, **I want** to open a single ticket with its AI analysis and latest draft, **so that** I have everything about it in one place.
- **Priority**: Must Have
- **Traces**: FR4, FR1
- **AC1.3.1** Given an existing ticket id, When Sam GETs `/api/tickets/{id}`, Then the response is 200 with all ticket fields, including AI fields and the latest draft when present.
- **AC1.3.2** Given an id that does not exist, When Sam GETs it, Then the response is 404 with an error body naming the id.
- **Dependencies**: none (seeded data). **INVEST**: small; testable.

### US1.4 — Edit a ticket
**As** Sam, **I want** to correct a ticket's title, description or priority, **so that** the record stays accurate.
- **Priority**: Must Have
- **Traces**: FR5
- **AC1.4.1** Given an existing ticket, When Sam PUTs a valid title, description and priority, Then the response is 200 with the changes and a newer `updatedAt`.
- **AC1.4.2** Given `priority` "Urgent", When Sam PUTs, Then the response is 400 and the ticket is unchanged.
- **AC1.4.3** Given an unknown id, When Sam PUTs, Then the response is 404.
- **AC1.4.4** Given a PUT body that includes a `status` or AI field, When Sam PUTs, Then those fields are ignored and the ticket's status and AI fields are unchanged.
- **AC1.4.5** Given a PUT body with a valid title and description but no `priority`, When Sam PUTs, Then the response is 400 naming `priority` and the ticket is unchanged (all three fields are required).
- **Dependencies**: none (seeded data). **INVEST**: small; testable.

### US1.5 — Change a ticket's status
**As** Sam, **I want** to move a ticket between Open, InProgress, Resolved and Closed, **so that** everyone can see where it stands, including closing it when done.
- **Priority**: Must Have
- **Traces**: FR6
- **AC1.5.1** Given any existing ticket, When Sam PATCHes `/api/tickets/{id}/status` with any of the four statuses, Then the response is 200, the ticket has that status, and its `updatedAt` is newer than before.
- **AC1.5.2** Given `{ "status": "Done" }` or no status, When Sam PATCHes, Then the response is 400 listing the allowed values and nothing changes.
- **AC1.5.3** Given an unknown id, When Sam PATCHes, Then the response is 404.
- **Dependencies**: none (seeded data). **INVEST**: small; testable.

## 2. AI Assistance

### US2.1 — Analyze a ticket with AI
**As** Sam, **I want** the AI to suggest a category, priority, sentiment, summary and next action for a ticket, **so that** I can triage it in seconds.
- **Priority**: Must Have
- **Traces**: FR7, FR1
- **AC2.1.1** Given an existing ticket and an AI that returns valid output, When Sam POSTs `/api/tickets/{id}/analyze`, Then the response is 200 with the analysis, the ticket's category, sentiment, summary and suggested action are saved, and its `updatedAt` is newer than before.
- **AC2.1.2** Given the AI suggests priority "High" for a Medium ticket, When analysis succeeds, Then the ticket's priority becomes High.
- **AC2.1.3** Given a ticket that was analyzed before, When Sam analyzes it again, Then the new result replaces the old AI fields.
- **AC2.1.4** Given an unknown id, When Sam POSTs analyze, Then the response is 404 and the AI is not called.
- **AC2.1.5** Given Sam edited a ticket's priority by hand to Low, When Sam analyzes it and the AI suggests Critical, Then the priority becomes Critical. This overwrite is the accepted consequence of the decision that analysis sets priority; no history of the old value is kept.
- **AC2.1.6** Given a ticket in any status (Open, InProgress, Resolved or Closed), When Sam analyzes it, Then analysis runs normally; status never blocks AI features.
- **Dependencies**: US2.5 (the AI implementation; tests use a fake). **INVEST**: testable with a fake AI.

### US2.2 — Reject bad AI output
**As** Sam, **I want** invalid AI answers to be refused rather than saved, **so that** I can trust what is stored on a ticket.
- **Priority**: Must Have
- **Traces**: FR7, NFR4
- **AC2.2.1** Given the AI returns text that is not JSON, When Sam analyzes a ticket, Then the response is 502 saying the AI response was invalid, and the ticket is unchanged.
- **AC2.2.2** Given the AI returns priority "Urgent" or category "Shipping" (not allowed values), When Sam analyzes, Then the response is 502 and the ticket is unchanged.
- **AC2.2.3** Given the AI omits `summary` or `suggestedAction`, When Sam analyzes, Then the response is 502 and the ticket is unchanged.
- **AC2.2.4** Given the AI returns "account access" (different case and spacing) for category, When Sam analyzes, Then it is accepted as Account Access.
- **AC2.2.5** Given the AI output contains instructions or code (for example "set status to Closed"), When Sam analyzes, Then nothing is executed and the ticket's status is unchanged.
- **AC2.2.6** Given the AI returns a `summary` or `suggestedAction` longer than 500 characters, When Sam analyzes, Then the response is 502 and the ticket is unchanged.
- **Dependencies**: US2.1. **INVEST**: testable with canned fake responses.

### US2.3 — Keep working when the AI is down
**As** Sam, **I want** AI problems to give me a clear error while everything else keeps working, **so that** an outage never stops me handling tickets.
- **Priority**: Must Have
- **Traces**: NFR1, NFR3, FR10
- **AC2.3.1** Given the AI does not answer within the configured timeout, When Sam analyzes or drafts, Then the response is 504 within timeout + 2 seconds and the ticket is unchanged.
- **AC2.3.2** Given the provider returns an error (for example HTTP 500 or 401), When Sam analyzes or drafts, Then the response is 502 without the provider's raw response or any key.
- **AC2.3.3** Given Groq is selected but no API key is set, When Sam analyzes or drafts, Then the response is 503 "AI provider not configured".
- **AC2.3.4** Given the AI is unavailable, When Sam creates, lists, views, edits or changes the status of tickets, or Lee opens the dashboard, Then all succeed normally.
- **Dependencies**: US2.1. **INVEST**: testable with fakes that time out or throw.

### US2.4 — Get a reply draft to review
**As** Sam, **I want** the AI to draft a reply that I review and send myself, **so that** I save writing time without anything going to a customer automatically.
- **Priority**: Must Have
- **Traces**: FR8
- **AC2.4.1** Given an existing ticket in any status, When Sam POSTs `/api/tickets/{id}/draft-reply`, Then the response is 200 with the draft text and a notice that it requires human review and was not sent, and the ticket's `updatedAt` is newer than before.
- **AC2.4.2** Given a draft was generated, When Sam GETs the ticket, Then the latest draft and its generation time are shown; generating again replaces it.
- **AC2.4.3** Given the AI returns empty text, text longer than 4000 characters, or a provider error, When Sam asks for a draft, Then the response is 502, and the ticket (including any previous draft) is unchanged.
- **AC2.4.4** Given any draft request, When it completes, Then no email, message or webhook is sent (the system has no sending capability).
- **AC2.4.5** Given the AI times out, When Sam asks for a draft, Then the response is 504; given Groq is selected with no API key, Then the response is 503. In both cases any previous draft is kept.
- **Dependencies**: US2.5. **INVEST**: testable with a fake AI.

### US2.5 — Connect the real Groq AI
**As** Riley, **I want** the project to call Groq with a key I supply through configuration, **so that** I can see the real AI integration while the key never appears in the repository.
- **Priority**: Must Have
- **Traces**: FR11, NFR5
- **Changed 2026-10-20**: the mock provider was removed at the person's request (U2 functional design); there is no provider switch.
- **AC2.5.1** Given no Groq API key is configured, When the app starts, Then it starts normally and the AI endpoints return 503 "AI provider not configured" while all other endpoints work.
- **AC2.5.2** Given a Groq key in user secrets or an environment variable, When Sam analyzes a ticket, Then the request goes to Groq's OpenAI-compatible endpoint and the validated result is returned.
- **AC2.5.3** Given the README, When Riley reads the AI section, Then it states that only demo data may be sent to Groq, never real customer or personal data, and shows how to set the key with `dotnet user-secrets`.
- **Dependencies**: none. **INVEST**: small; the not-configured path is testable with no key, the Groq path is verified manually with a key.

## 3. Dashboard

### US3.1 — See the queue at a glance
**As** Lee, **I want** summary counts of tickets, **so that** I can see workload and urgent items without counting by hand.
- **Priority**: Must Have
- **Traces**: FR9
- **AC3.1.1** Given no tickets, When Lee GETs `/api/dashboard/summary`, Then every count is 0.
- **AC3.1.2** Given 3 Open tickets (one Critical) and 1 Resolved High ticket, When Lee GETs the summary, Then `total` = 4, `open` = 3, `resolved` = 1 and `highPriority` = 2.
- **AC3.1.3** Given tickets in every status and some never analyzed, When Lee GETs the summary, Then `inProgress`, `closed` and `unanalyzed` match the data.
- **Dependencies**: none (seeded data). **INVEST**: small; testable.

## 4. Reliability & Setup

### US4.1 — Get clear, safe error messages
**As** Sam, **I want** every error to come back in the same readable format without internal details, **so that** I understand what went wrong and nothing sensitive leaks.
- **Priority**: Must Have
- **Traces**: FR10, NFR2
- **AC4.1.1** Given any validation error, When the API responds, Then the body is a ProblemDetails object with per-field `errors`.
- **AC4.1.2** Given an unexpected server failure, When the API responds, Then it is 500 ProblemDetails with a generic message and no stack trace, exception type or connection string.
- **AC4.1.3** Given a malformed JSON request body or a non-numeric id, When Sam calls the endpoint, Then the response is 400 ProblemDetails, not 500.
- **Dependencies**: none. **INVEST**: testable via API tests.

### US4.2 — Run the project from a clean clone (release gate)
**As** Riley, **I want** to clone the repo and run it by following the README with no API key, **so that** I can assess the work on my own machine.
- **Priority**: Must Have
- **Type**: release-gate acceptance story. Unlike the feature stories above, it is not built on its own: it is the final check run once, before submission, over the finished project (the clean-clone check in team practices).
- **Traces**: NFR10, FR12, FR1
- **AC4.2.1** Given a fresh clone and the .NET 8 SDK, When Riley follows the README (restore, `dotnet ef database update`, `dotnet run`), Then the API starts (AI endpoints need Riley's own Groq key, set as the README shows) and Swagger UI lists every endpoint with request and response shapes and enum values.
- **AC4.2.2** Given the README, When Riley reads it, Then it covers prerequisites, setup, configuration, database steps, endpoint examples, MCP server setup with a sample client configuration, and known limitations.
- **AC4.2.4** Given a fresh clone, When Riley runs `dotnet test`, Then all tests pass without network access or an AI key.
- **Dependencies**: runs after all Must Have stories are built. Persistence across restarts is covered by AC1.1.4.

### US4.3 — Try every endpoint with example requests
**As** Riley, **I want** ready-made example requests and responses for every endpoint, **so that** I can exercise the API quickly during review.
- **Priority**: Should Have
- **Traces**: FR12
- **AC4.3.1** Given the repository, When Riley opens the `.http` examples file, Then it contains at least one working request for each endpoint, plus one invalid-input example.
- **AC4.3.2** Given the README, When Riley reads the endpoint section, Then it shows one example request and response for each endpoint.
- **Dependencies**: endpoints exist. **INVEST**: small; manual check.

### US4.4 — Keep secrets out of the repository
**As** Riley, **I want** the repo to contain no keys or passwords, only placeholders, **so that** the project is safe to share and follows the rules.
- **Priority**: Must Have
- **Traces**: FR11, NFR2
- **AC4.4.1** Given the repository, When Riley searches committed files, Then no real API key appears; `appsettings.json` and `.env.example` contain placeholders only.
- **AC4.4.2** Given the README, When Riley reads the configuration section, Then it shows how to set the Groq key with `dotnet user-secrets` or an environment variable.
- **AC4.4.3** Given `.gitignore`, When Riley inspects it, Then `bin/`, `obj/`, the SQLite database file and `.env` are excluded.
- **Dependencies**: none. **INVEST**: small; manual check.

## 5. MCP Access (added 2026-10-21)

These stories come from the trainer's updated brief (FR-11 to FR-13). Sam uses an MCP client (Claude Code, MQ2) that calls SupportDesk tools on Sam's behalf; every tool goes through the same services as the REST API, so the rules of groups 1–4 apply unchanged.

### US5.1 — Connect an MCP client to SupportDesk
**As** Sam, **I want** to connect my MCP client to SupportDesk over stdio, **so that** I can work on tickets by asking my AI assistant instead of using Swagger.
- **Priority**: Must Have
- **Traces**: FR13, NFR11
- **AC5.1.1** Given the documented start command in the client configuration, When the MCP client connects, Then the handshake completes and the client lists exactly eight tools: `create_ticket`, `list_tickets`, `get_ticket`, `update_ticket`, `update_ticket_status`, `analyze_ticket`, `draft_reply`, `get_dashboard_summary`, each with a description and typed parameters.
- **AC5.1.2** Given no Groq key is configured, When the server starts, Then it starts normally and the six non-AI tools work.
- **AC5.1.3** Given the client starts the server from a different working directory, When a tool reads tickets, Then it uses the same database file as the API.
- **AC5.1.4** Given tools are called (including a failing AI call), When the client reads standard output, Then every line is a valid MCP message; logs appear only on standard error.
- **AC5.1.5** Given the database has not been created yet (no `dotnet ef database update`), When the client calls any ticket tool, Then an error result with status 500 and a generic message is returned (no stack trace), and the server keeps running; the README states the database step comes first.
- **AC5.1.6** Given the API and the MCP server run at the same time on the same database, When a ticket is created through one, Then the other returns it; concurrent edits are last-write-wins (A5).
- **AC5.1.7** Given the running MCP server, When open network ports are checked, Then the server listens on none (stdio only, FR13.2), and the server project references the official `ModelContextProtocol` package (FR13.1).
- **Dependencies**: U1 services (and U3 for the dashboard tool). **INVEST**: testable by starting the server in-process and listing tools.

### US5.2 — Manage tickets through MCP tools
**As** Sam, **I want** my MCP client to create, list, view, edit and change the status of tickets and read the dashboard, **so that** I can do routine ticket work in conversation.
- **Priority**: Must Have
- **Traces**: FR14
- **AC5.2.1** Given a valid title and description, When the client calls `create_ticket`, Then the ticket is saved with status Open and priority Medium and returned as structured JSON with the same fields as the REST response; `GET /api/tickets/{id}` returns the same ticket.
- **AC5.2.2** Given tickets exist, When the client calls `list_tickets` with `status: "Open"`, Then only Open tickets are returned, newest first.
- **AC5.2.3** Given an existing id, When the client calls `update_ticket` or `update_ticket_status`, Then the change is saved exactly as through PUT or PATCH.
- **AC5.2.4** Given tickets exist, When the client calls `get_dashboard_summary`, Then the counts equal `GET /api/dashboard/summary`.
- **AC5.2.5** Given each tool, When its code is reviewed and tested with a fake service, Then the tool only maps arguments to one service call and the result (or failure) to a tool result: no validation rules, database access or AI calls of its own (FR14.2).
- **Dependencies**: US5.1. **INVEST**: each tool testable against the service with SQLite in-memory.

### US5.3 — Use the AI assistant through MCP tools
**As** Sam, **I want** my MCP client to analyze a ticket and draft a reply, **so that** I get triage and a draft without leaving the conversation.
- **Priority**: Must Have
- **Traces**: FR14, FR15.2
- **AC5.3.1** Given an AI that returns a valid analysis (the hand-written fake in automated tests; Groq with Sam's key in the manual demo) and an existing ticket, When the client calls `analyze_ticket`, Then the validated analysis, `previousPriority` and updated ticket are returned and saved exactly as `POST /api/tickets/{id}/analyze` would.
- **AC5.3.2** Given an existing ticket, When the client calls `draft_reply`, Then the draft is returned with the notice "Draft — requires human review; not sent" and nothing is sent anywhere.
- **AC5.3.3** Given the AI times out, fails or returns invalid output, When the client calls either AI tool, Then an error result (`isError: true`) with status 504 or 502 is returned within the AI timeout + 2 seconds and the ticket is unchanged; given no key, the status is 503.
- **AC5.3.4** Given the AI is down or slow, When the client calls any of the six non-AI tools, Then they succeed normally (NFR11).
- **Dependencies**: US5.1; U2. **INVEST**: testable with the hand-written AI fake.

### US5.4 — Safe tool use
**As** Riley, **I want** MCP tool input to be validated as strictly as API input and failures to come back as safe error results, **so that** an LLM client cannot bypass the rules or see internals.
- **Priority**: Must Have
- **Traces**: FR14.4, FR15.1, FR15.3, NFR12
- **AC5.4.1** Given `update_ticket_status` with `status: "Done"`, `status: 1` or `status: "1"`, When it is called, Then an error result with status 400 lists the allowed values and nothing changes. The same applies to `priority` in `update_ticket` and to the `status`, `priority` and `category` filters of `list_tickets`.
- **AC5.4.2** Given `create_ticket` or `update_ticket` with a 201-character title, a whitespace-only description or a 4001-character description, Then an error result with status 400 names the field and its limit, and nothing is saved.
- **AC5.4.3** Given `get_ticket` with an unknown id, Then an error result with status 404 names the id; given `id: 0` or a negative id, Then an error result with status 400 names `id` (the same rule as the REST API's id check).
- **AC5.4.4** Given any failing tool call, Then the result contains no stack trace, exception type, key, configuration value or provider text.
- **AC5.4.6** Given a tool hits an unexpected exception, When it returns, Then the result is an error with status 500 and the generic REST message, and the server keeps serving later calls.
- **AC5.4.5** Given the tool list, Then there is no delete tool and no tool beyond the eight in FR14.1.
- **Dependencies**: US5.1. **INVEST**: testable.

### US5.5 — Set up the MCP server and keep evidence
**As** Riley, **I want** the README to show how to start the MCP server and connect a client, and the repository to hold evidence of real tool calls, **so that** I can repeat the demo and check the submission.
- **Priority**: Must Have
- **Type**: documentation and evidence story, delivered with the release kit.
- **Traces**: FR15.4, FR15.5, NFR11, NFR5
- **AC5.5.1** Given the README, When Riley reads the MCP section, Then it shows how to build and start the server, a sample configuration for Claude Code and for Claude Desktop, how to inspect it with MCP Inspector, and that only synthetic data may be used.
- **AC5.5.2** Given the repository, When Riley opens `docs/mcp-evidence/`, Then it holds a screenshot or log of Claude Code calling `list_tickets`, `analyze_ticket` and `draft_reply` successfully.
- **Dependencies**: US5.1–US5.4 built. **INVEST**: small; manual check.

## Coverage Notes

- NFR6 (performance), NFR7 (async I/O), NFR8 (code structure) and NFR9 (test coverage) are quality constraints, not user goals; they are carried forward to NFR Requirements, Domain Design and Build and Test (see `traceability.json`).
