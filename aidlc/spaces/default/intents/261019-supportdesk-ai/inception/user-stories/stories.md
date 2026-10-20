# User Stories — SupportDesk AI

Source: `inception/requirements-analysis/requirements.md` (FR1–FR12, NFR1–NFR10), personas in `personas.md`, plan answers in `user-stories-questions.md`. Testing posture (test-after, xUnit, fakes for the AI) from `inception/practices-discovery/team-practices.md`: every acceptance criterion below is written so it can become an automated test that never calls a real AI.

Priorities use MoSCoW. The MVP line is decided in Delivery Planning.

## Story Map

| Group | Must Have | Should Have | Won't Have (this time) |
|---|---|---|---|
| 1 Ticket Management | US1.1 Create, US1.2 List & filter, US1.3 View, US1.4 Edit, US1.5 Change status | — | Delete a ticket; pagination; sorting options; full-text search |
| 2 AI Assistance | US2.1 Analyze, US2.2 Reject bad AI output, US2.3 Survive AI outages, US2.4 Draft reply, US2.5 Choose mock or Groq | — | Sending replies (email, chat, webhooks); automatic AI retries |
| 3 Dashboard | US3.1 Summary counts | — | Charts or trends over time |
| 4 Reliability & Setup | US4.1 Consistent safe errors, US4.2 Run from a clean clone (release gate), US4.4 Secrets stay out of Git | US4.3 Example requests | Authentication; frontend UI; CI pipeline |

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
- **Dependencies**: US2.5 (an AI implementation, the mock suffices). **INVEST**: testable with a fake AI.

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

### US2.5 — Choose the mock or the real Groq AI
**As** Riley, **I want** the project to use a clearly labelled mock AI by default and switch to Groq by configuration, **so that** I can run it without a key and still see the real integration when a key is provided.
- **Priority**: Must Have
- **Traces**: FR11, NFR5
- **AC2.5.1** Given no AI configuration, When the app starts, Then the mock AI is used and its results are visibly marked as mock (e.g. `[MOCK]`).
- **AC2.5.2** Given `Ai:Provider` = Groq and a key in user secrets or an environment variable, When Sam analyzes a ticket, Then the request goes to Groq's OpenAI-compatible endpoint and the validated result is returned.
- **AC2.5.3** Given the README, When Riley reads the AI section, Then it states that only demo data may be sent to Groq, never real customer or personal data.
- **Dependencies**: none. **INVEST**: small; mock path testable, Groq path verified manually with a key.

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
- **AC4.2.1** Given a fresh clone and the .NET 8 SDK, When Riley follows the README (restore, `dotnet ef database update`, `dotnet run`), Then the API starts with the mock AI and Swagger UI lists every endpoint with request and response shapes and enum values.
- **AC4.2.2** Given the README, When Riley reads it, Then it covers prerequisites, setup, configuration, database steps, endpoint examples and known limitations.
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

## Coverage Notes

- NFR6 (performance), NFR7 (async I/O), NFR8 (code structure) and NFR9 (test coverage) are quality constraints, not user goals; they are carried forward to NFR Requirements, Domain Design and Build and Test (see `traceability.json`).
