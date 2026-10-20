# Units of Work — SupportDesk AI

Sources: components in `inception/domain-design/components.md` (Tickets, AiAssistant, Dashboard), decisions ADR-001..005 in `inception/domain-design/decisions.md`, requirements FR1–FR12 / NFR1–NFR10 in `inception/requirements-analysis/requirements.md`, stories US1.1–US4.4 in `inception/user-stories/stories.md`, and the answers in `units-generation-questions.md`.

**Deployment model (all units):** one ASP.NET Core 8 Web API project (a modular monolith, single deployable) plus one xUnit test project. Every unit is code *embedded* in that one project; units are built, tested and merged on their own short-lived branch.

## Unit Index

| Unit ID | Directory | Name | Kind | Components | Complexity |
|---|---|---|---|---|---|
| U1 | `u1-foundation-tickets` | Foundation & Tickets | service | Tickets (CRUD, status, filters), shared plumbing | L |
| U2 | `u2-ai-assistant` | AI Assistant | service | AiAssistant; Tickets' analyze and draft-reply flows | L |
| U3 | `u3-dashboard` | Dashboard | service | Dashboard | S |
| U4 | `u4-release-kit` | Release Kit | packaging | — (documentation and configuration deliverables) | S |

## U1 — Foundation & Tickets (`u1-foundation-tickets`)

- **Description**: the runnable base of the application and full ticket management. Starts as the thin end-to-end slice from team practices (create, list and get through controller, service and SQLite, surviving a restart), then grows to update, status and filters.
- **Boundaries**: owns the Ticket entity and every write to it except the AI-result fields (written by U2's flows inside the Tickets service). Owns the shared plumbing: global exception handler (ADR-005), ProblemDetails setup, Swagger, configuration binding, EF Core DbContext and migrations, and the shared fixed-value types (Status, Priority, Category, Sentiment) in one shared Domain folder.
- **Responsibilities / delivers**:
  - Solution, Web API project, xUnit test project, `.editorconfig`, standard .NET `.gitignore` entries.
  - EF Core + SQLite, `Ticket` entity with all fields (including AI and draft fields, nullable), initial migration.
  - `POST /api/tickets`, `GET /api/tickets` (filters, ordering), `GET /api/tickets/{id}`, `PUT /api/tickets/{id}`, `PATCH /api/tickets/{id}/status`.
  - Input validation and the global exception handler (400/404/500 ProblemDetails; typed AI failures are mapped here too so U2 only throws them).
- **Deployment**: embedded in the single Web API project.
- **Kind**: service. **Complexity**: L.
- **Implementation notes / constraints**: thin controllers; services use the DbContext directly (ADR-004); enums serialised as strings; tests with SQLite in-memory and `WebApplicationFactory`; must build and pass tests before merge (team practices).

## U2 — AI Assistant (`u2-ai-assistant`)

- **Description**: everything about the AI, plus the two ticket flows that use it.
- **Boundaries**: owns the AiAssistant component (ADR-002): `IAiTicketService`, Mock and Groq providers, prompts, the shared validator, timeouts and typed failures. Adds the analyze and draft-reply operations to the Tickets service and their endpoints; writes only the AI and draft fields of Ticket.
- **Responsibilities / delivers**:
  - `IAiTicketService` with analyze and draft operations; `MockAiTicketService` (default, labelled `[MOCK]`) and `GroqAiTicketService` (OpenAI-compatible API), selected by configuration.
  - Validator for analysis (allowed values, lengths, JSON shape) and drafts (1–4000 chars).
  - Timeout (default 20 s) and cancellation; typed failures: invalid output, timeout, provider error, not configured.
  - `POST /api/tickets/{id}/analyze` and `POST /api/tickets/{id}/draft-reply`, including priority overwrite and draft storage.
- **Deployment**: embedded in the single Web API project; Groq reached over HTTPS.
- **Kind**: service. **Complexity**: L.
- **Implementation notes / constraints**: API key only from user secrets or environment variables; never logged or returned; tests use hand-written fakes and never call Groq; prompts wrap ticket text as data.

## U3 — Dashboard (`u3-dashboard`)

- **Description**: summary counts for the support lead.
- **Boundaries**: owns the Dashboard component (ADR-003); read-only queries over the Ticket table; never writes.
- **Responsibilities / delivers**: `GET /api/dashboard/summary` returning total, open, inProgress, resolved, closed, highPriority, unanalyzed.
- **Deployment**: embedded in the single Web API project.
- **Kind**: service. **Complexity**: S.
- **Implementation notes / constraints**: never calls the AI; all zeros when empty.

## U4 — Release Kit (`u4-release-kit`)

- **Description**: what a reviewer needs to run and assess the project.
- **Boundaries**: documentation and configuration files only; no business logic.
- **Responsibilities / delivers**: README (prerequisites, setup, configuration incl. Groq key via user secrets, `dotnet ef database update`, endpoint examples, known limitations, demo-data-only warning), `.http` file with a request per endpoint plus an invalid example, `appsettings.json` / `.env.example` placeholders only, secrets check, and the clean-clone release-gate check (US4.2).
- **Deployment**: files in the repository.
- **Kind**: packaging. **Complexity**: S.
- **Implementation notes / constraints**: written against the finished endpoints; contains no real keys.
