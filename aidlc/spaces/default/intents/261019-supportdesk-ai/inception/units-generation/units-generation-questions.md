# Units Generation — Questions

Inputs: `inception/domain-design/components.md` (Tickets, AiAssistant, Dashboard), `inception/domain-design/decisions.md` (ADR-001..005), `inception/requirements-analysis/requirements.md`, `inception/user-stories/stories.md` (US1.1–US4.4), and the affirmed practice "build a thin end-to-end slice first: create/list/get through controller, service and SQLite, plus Swagger and .gitignore, surviving a restart".

A unit of work is a piece you build, test and merge on its own branch. This stage decides the pieces and what depends on what; the build order is chosen later in Delivery Planning.

### Q1. How should the work be split into units?
A. (Suggested) Four units: **U1 Foundation & Tickets** (solution + test project, EF Core/SQLite + first migration, error handler, Swagger, .gitignore, shared enums, all ticket endpoints — the thin slice grows into full ticket management), **U2 AI Assistant** (IAiTicketService, Mock + Groq, validation, timeouts, plus the analyze and draft-reply endpoints), **U3 Dashboard** (summary endpoint), **U4 Release Kit** (README, `.http` examples, config placeholders, clean-clone check)
B. Three units: same as A, but README/examples are written inside each unit instead of a separate U4
C. Five units: same as A, but split U1 into **U1 Thin slice** (create/list/get only) and **U1b Ticket updates** (PUT, status, filters)
X. Other (please specify)

[Answer]: A

### Q2. How is the application deployed?
A. (Suggested) One ASP.NET Core app (single deployable, a modular monolith): every unit is code inside the same Web API project, with one test project
B. Separate class-library projects per component (e.g. `SupportDesk.Tickets`, `SupportDesk.Ai`) referenced by one Web API project
X. Other (please specify)

[Answer]: A

### Q3. Can the AI work and the Dashboard be built independently of each other?
Both only need U1's ticket data; neither uses the other.

A. (Suggested) Yes: U2 and U3 depend only on U1, so either can be built first; U4 depends on all three
B. No: make U3 depend on U2 so the dashboard's "unanalyzed" count is built after analysis exists
X. Other (please specify)

[Answer]: A
