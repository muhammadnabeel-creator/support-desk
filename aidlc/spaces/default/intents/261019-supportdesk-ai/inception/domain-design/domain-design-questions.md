# Domain Design — Questions

Inputs: `inception/requirements-analysis/requirements.md`, `inception/user-stories/stories.md`, `inception/practices-discovery/team-practices.md` ("thin controllers, business logic in services registered through DI, DTOs separate from EF entities, no architecture layers the brief does not need").

These questions decide the logical building blocks (the code you will write) and who owns what. Hosting, tech-stack details and NFR patterns come in later stages.

### Q1. How many building blocks should the code be split into?
This decides how you explain the controller -> service -> database/AI flow in your walkthrough.

A. (Suggested) Three: **Tickets** (owns the Ticket data; create/list/get/update/status, and runs the analyze and draft-reply flows by calling the AI), **AiAssistant** (the `IAiTicketService` contract, the Mock and Groq implementations, prompts and AI-output validation), **Dashboard** (summary counts). Error handling is shared plumbing, not a block.
B. Four: same as A, plus a separate **TicketAiWorkflow** block that orchestrates analyze/draft (load ticket -> call AI -> validate -> save), so Tickets stays pure CRUD
C. Two: **Tickets** (including dashboard counts) and **AiAssistant**
X. Other (please specify)

[Answer]: A

### Q2. Where should AI output be validated?
Rule: never save or return AI output that has not been validated against a typed DTO and allowed values.

A. (Suggested) Inside **AiAssistant**: one shared validator turns raw AI text into a typed, checked result (or a clear "invalid AI output" error) for both Mock and Groq, so Tickets only ever receives validated data
B. Inside **Tickets**: AiAssistant returns raw parsed JSON and the ticket service checks it before saving
X. Other (please specify)

[Answer]: A

### Q3. How does Dashboard get its numbers?
A. (Suggested) Dashboard asks for counts with read-only database queries over the Ticket table (it never writes); Tickets remains the only writer of ticket data
B. Dashboard calls a counting method on the Tickets service (no direct database access at all)
X. Other (please specify)

[Answer]: A

### Q4. How do services talk to the database?
A. (Suggested) Use the EF Core `DbContext` directly inside services (EF already acts as repository + unit of work); no extra repository layer
B. Add a repository interface (`ITicketRepository`) between services and EF
X. Other (please specify)

[Answer]: A

### Q5. How are errors turned into HTTP responses?
A. (Suggested) Services throw a small set of meaningful exceptions (ticket not found, AI timeout, invalid AI output, AI provider error, AI not configured); one global exception handler maps them to ProblemDetails with the right status code (404/504/502/502/503) and hides everything else as 500
B. Services return a Result object (success or error code) and each controller maps it to a response
X. Other (please specify)

[Answer]: A

## Change Request 2026-10-21 — MCP server

### MQ4. Where should the MCP server live in the solution?
The brief allows "a separate project in the solution or a startup mode of the API host". The tools must call the same services as the REST API (FR14.2).

A. (Suggested) A separate console project `src/SupportDesk.Mcp` that references the API project and reuses its services, database setup and AI configuration through one shared registration method; the API stays unchanged apart from moving its service registration into that method
B. A startup mode of the API: `dotnet run --project src/SupportDesk.Api -- --mcp` starts an MCP host instead of the web server (one project, but Program.cs branches between two hosts)
C. Move services, data and AI into a new class library `SupportDesk.Core` referenced by both the API and a separate MCP project (cleanest split, but refactors merged code and adds a layer)
X. Other (please specify)

[Answer]: X — we need to keep it in dsame fodler as apis also the tools should call the apis we wrote

### MFQ1. Inside the API project, what exactly should each tool call?
You chose to keep the MCP server in the API project (`src/SupportDesk.Api/Mcp/`, started with `dotnet run --project src/SupportDesk.Api -- --mcp`). "Call the APIs we wrote" can mean two things. The brief (FR-12) says each tool "calls the same service layer as the REST endpoints".

A. (Suggested) Each tool calls the same C# service methods the controllers call (`ITicketService`, the dashboard service): no HTTP in between, works without the web API running, and matches FR-12 word for word
B. Each tool sends an HTTP request to our REST endpoints (`http://localhost:5189/api/...`): the web API must be running at the same time, and errors come back as ProblemDetails to translate
X. Other (please specify)

[Answer]: A
