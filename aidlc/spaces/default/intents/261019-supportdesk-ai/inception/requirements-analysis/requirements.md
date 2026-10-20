# Requirements — SupportDesk AI

## Intent Analysis

SupportDesk AI is a support-ticket management Web API built as a trainee assessment. The goal is not a large feature set but a **reliable, explainable MVP** that shows the trainee can apply ASP.NET Core concepts (controllers, DI, async, EF Core, DTOs, validation, error handling) and safely integrate an AI capability. The reviewer must be able to clone and run it from the README, and the trainee must be able to explain the controller -> service -> database/AI flow and make a small change live. [desc]

- **Request type**: new greenfield application, multi-component (API, persistence, AI integration, tests). [desc]
- **Complexity / depth**: Standard. Endpoints are well defined by the brief; the open decisions were settled in `requirements-analysis-questions.md`. [scope]
- **Primary actor**: a support agent using the API through Swagger (no frontend required). [desc]
- **Success**: all ten brief requirements (FR-01..FR-10) work, data persists across restarts, AI output is validated or rejected in a controlled way, and the definition of done in the brief is met within 5 days. [desc]

Working practices (branching, test-after with xUnit, 80% coverage floor, local checks in place of CI, `.editorconfig` + `dotnet format`, documented `dotnet ef database update`) come from the affirmed `team-practices.md` and are not repeated here except where they constrain a requirement. [memory:team-practices]

## Functional Requirements

Brief IDs (FR-01..FR-10) are kept in brackets for traceability to the source document.

### FR1 — Ticket data model [brief §5; Q2, Q3, Q4, Q6, Q7]
- **FR1.1** A Ticket has: `Id` (integer, database-generated), `Title`, `Description`, `Status`, `Priority`, `Category` (nullable), `Sentiment` (nullable), `AiSummary` (nullable), `SuggestedAction` (nullable), `DraftReply` (nullable), `DraftReplyGeneratedAt` (nullable, UTC), `CreatedAt` (UTC), `UpdatedAt` (UTC).
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
- **FR3.3** No pagination in this version.
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
- **FR8.2** The draft is validated (non-empty, max 4000 characters), stored on the ticket as `DraftReply` with `DraftReplyGeneratedAt`, replacing any earlier draft, and returned with `"status": "Draft — requires human review; not sent"`. [Q4]
- **FR8.3** The system has **no** capability to send a reply (no email, webhook or messaging integration). [rule: never auto-send]
- **AC8.1** Given an existing ticket, Then 200 with the draft text and the review notice; GET of the ticket shows the same draft. Given the AI fails or returns empty text, Then no draft is saved and a controlled error is returned.

### FR9 — Dashboard summary [FR-08]
- **FR9.1** `GET /api/dashboard/summary` returns counts: `total`, `open` (Status = Open), `inProgress`, `resolved`, `closed`, `highPriority` (Priority = High or Critical, any status), and `unanalyzed` (Category is null).
- **AC9.1** Given no tickets, Then all counts are 0. Given 3 Open (one Critical) and 1 Resolved High ticket, Then `total = 4`, `open = 3`, `resolved = 1`, `highPriority = 2`.

### FR10 — Error responses [FR-09; NFR2]
- **FR10.1** All errors use one consistent JSON shape: RFC 7807 `ProblemDetails` (`type`, `title`, `status`, `detail`, plus `errors` per field for validation).
- **FR10.2** Status codes: 400 validation / invalid enum value; 404 unknown ticket; 502 AI provider error or invalid AI output; 504 AI timeout; 503 AI not configured (e.g. Groq selected but no key); 500 unexpected error.
- **FR10.3** Error bodies never contain stack traces, exception type names, connection strings, API keys or raw provider responses. Details are logged server-side only (without secrets).
- **AC10.1** Given an unhandled exception, Then the client receives a 500 ProblemDetails with a generic message and no stack trace.

### FR11 — AI provider and configuration [FR-10; FQ1]
- **FR11.1** AI access goes through an `IAiTicketService` interface with two implementations: `MockAiTicketService` (clearly labelled mock, deterministic, no network) and `GroqAiTicketService` (Groq's OpenAI-compatible chat completions API, `https://api.groq.com/openai/v1`). [FQ1]
- **FR11.2** The active implementation is selected by configuration (`Ai:Provider` = `Mock` or `Groq`). Default when not set: `Mock`.
- **FR11.3** Groq settings: `Ai:Groq:ApiKey` (from `dotnet user-secrets` or environment variable only), `Ai:Groq:Model`, `Ai:Groq:BaseUrl`, `Ai:TimeoutSeconds`. Committed `appsettings.json` and `.env.example` hold placeholders only.
- **FR11.4** Responses from the mock are visibly marked (e.g. summary prefixed `[MOCK]`) so a demo can never be mistaken for real AI output.
- **AC11.1** Given `Ai:Provider = Groq` and no API key, Then AI endpoints return 503 "AI provider not configured" and all ticket CRUD endpoints still work.

### FR12 — API exploration [brief §3]
- **FR12.1** Swagger / OpenAPI UI is available in the Development environment and documents every endpoint, request and response shape, and enum values.

## Non-Functional Requirements

- **NFR1 — Independence from AI**: create, list, get, update, status and dashboard endpoints never call the AI provider and work when it is down, slow or unconfigured. [rule: ALWAYS keep core operations working]
- **NFR2 — No leakage**: no secrets or stack traces in any HTTP response; secrets never logged; no secrets in the Git repository. [rules]
- **NFR3 — AI timeout**: every AI call has a configurable timeout (default 20 seconds) and honours the request `CancellationToken`; a timeout returns 504 within timeout + 2 seconds and leaves the ticket unchanged.
- **NFR4 — Untrusted AI output**: AI output is treated as untrusted data — never executed, never used as SQL, never allowed to set status. Ticket text sent to the AI is wrapped as data in the prompt, with instructions that the model returns JSON only. [rules]
- **NFR5 — Data protection**: only demo / synthetic ticket text is used with the real Groq provider; the README states that real customer or personal data must not be entered. [rule: never send personal data]
- **NFR6 — Performance (local)**: non-AI endpoints respond in under 500 ms for up to 1,000 tickets on a developer machine.
- **NFR7 — Async I/O**: all database and HTTP calls are async and pass a `CancellationToken`.
- **NFR8 — Maintainability**: thin controllers; business logic in services registered through DI; DTOs separate from EF entities; no extra architecture layers. [memory:team-practices]
- **NFR9 — Testability**: automated xUnit tests cover the key business rules and all AI-output validation paths (valid, malformed JSON, invalid enum, missing field, timeout, provider error); tests never call the real AI; at least 80% line coverage on hand-written code. [memory:team-practices]
- **NFR10 — Reproducibility**: a reviewer can clone the repo and run it by following the README (prerequisites, setup, configuration, `dotnet ef database update`, endpoint examples, known limitations) with the mock provider and no API key.

## Constraints

- ASP.NET Core 8 Web API with controllers, C#, EF Core, SQLite, Swagger; .NET 8 SDK. [desc]
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

## Open Questions

- None blocking. The exact default Groq model name is chosen during Code Generation and documented in the README (A8).

## Sources

- `[desc]` Initial description: "Build SupportDesk AI, an ASP.NET Core 8 Web API with EF Core + SQLite, following the catalogued requirements document … Local only, no cloud deployment." and the catalogued brief `documents/supportdesk-requirements.pdf` (FR-01..FR-10, §3, §5, §7–§11).
- `[scope]` Workflow-selected scope: classic, Standard depth.
- `[Q1]`–`[Q8]`, `[FQ1]` — answers in `requirements-analysis-questions.md`.
- `[memory:team-practices]` — `inception/practices-discovery/team-practices.md` and the affirmed rules in `aidlc/spaces/default/memory/project.md`.
- `[assumption]` — defaults chosen by the product manager, listed under Assumptions for correction at approval.
