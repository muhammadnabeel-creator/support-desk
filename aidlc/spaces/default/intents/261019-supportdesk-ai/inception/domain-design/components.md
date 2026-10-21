# Components — SupportDesk AI

Sources: `inception/requirements-analysis/requirements.md` (FR1–FR15, NFR1–NFR12), `inception/user-stories/stories.md` (US1.1–US5.5; group 5 MCP added 2026-10-21), `inception/practices-discovery/team-practices.md` (thin controllers, services via DI, DTOs separate from entities, no unnecessary layers), and the answers in `domain-design-questions.md`. Decisions are recorded in `decisions.md` (ADR-001 to ADR-006).

## Component Catalogue

```yaml
components:
  - name: Tickets
    summary: Owns support tickets and every change to them, including applying validated AI results.
    behaviour: >
      Creates tickets with Status Open and Priority Medium after validating title (3-200 chars) and
      description (10-4000 chars) on trimmed text. Lists tickets newest CreatedAt first (ties by Id
      descending) with optional, case-insensitive status/priority/category filters. Gets one ticket or
      reports not-found. Updates title, description and priority only; status changes only through the
      status operation, which accepts any of Open, InProgress, Resolved, Closed from any status.
      Runs the analyze flow (load ticket -> ask AiAssistant -> receive an already-validated result ->
      save Category, Sentiment, AiSummary, SuggestedAction and overwrite Priority) and the draft flow
      (load ticket -> ask AiAssistant -> save DraftReply and DraftReplyGeneratedAt, replacing any
      earlier draft). AI features work for any status. Any AI failure leaves the ticket unchanged.
      Refreshes UpdatedAt on every change. Never sends a reply anywhere. Never calls the AI for
      create, list, get, update or status, so those keep working when the AI is down.
    responsibilities:
      - Ticket lifecycle and validation rules (FR2, FR5, FR6)
      - Ticket listing and filtering (FR3, FR4)
      - Orchestrating analyze and draft-reply and persisting their validated results (FR7, FR8)
      - Being the only writer of ticket data
    depends_on:
      - component: AiAssistant
        interaction: Requests a ticket analysis or a reply draft and receives a validated typed result or a typed AI failure
        style: sync
    dependents:
      - component: Dashboard
        interaction: Dashboard reads ticket data (read-only) to count tickets
    external_dependencies:
      - name: SQLite (via EF Core)
        kind: database
        purpose: Persist tickets across restarts
    entities:
      - name: Ticket
        identifier: Id
        attributes: [Id, Title, Description, Status, Priority, Category, Sentiment, AiSummary, SuggestedAction, DraftReply, DraftReplyGeneratedAt, CreatedAt, UpdatedAt]

  - name: AiAssistant
    summary: Talks to the AI provider and turns its untrusted output into validated, typed results.
    behaviour: >
      Exposes the IAiTicketService contract with two operations: analyze a ticket's title and
      description, and draft a reply. One provider implements it: Groq (OpenAI-compatible chat
      completions); there is no mock provider (decision 2026-10-20), and tests use hand-written fakes
      of the interface. Builds prompts that wrap ticket text as data and ask
      for JSON only. Applies a configurable timeout (default 20 s) and honours cancellation. Validates
      every response before returning it: must be JSON with category, priority, sentiment in the
      allowed lists (case- and space-tolerant), summary and suggestedAction 1-500 chars; drafts
      1-4000 chars. Anything else becomes an "invalid AI output" failure. Reports timeout, provider
      error and "not configured" (Groq with no key) as distinct failures. Never executes AI output,
      never logs or returns the API key or raw provider responses, holds no ticket data.
    responsibilities:
      - IAiTicketService contract (FR11)
      - Groq provider (FR11)
      - Prompt construction that treats ticket text as data (NFR4)
      - AI-output validation for analysis and drafts (FR7.2, FR8.2)
      - Timeout and failure classification (NFR3, FR10.2)
    depends_on: []
    dependents:
      - component: Tickets
        interaction: Tickets requests analyses and drafts
    external_dependencies:
      - name: Groq API (OpenAI-compatible chat completions)
        kind: third-party-api
        purpose: Real AI analysis and reply drafting when configured
    entities: []

  - name: Dashboard
    summary: Produces summary counts of tickets for the support lead.
    behaviour: >
      Returns total, open (Status Open), inProgress, resolved, closed, highPriority (Priority High or
      Critical, any status) and unanalyzed (Category not set). Uses read-only queries over ticket data
      and never writes. Returns all zeros when there are no tickets. Never calls the AI.
    responsibilities:
      - Dashboard summary counts (FR9)
    depends_on:
      - component: Tickets
        interaction: Reads ticket data owned by Tickets, read-only, to compute counts
        style: sync
    dependents:
      - component: McpServer
        interaction: McpServer asks for the summary counts for the get_dashboard_summary tool
    external_dependencies:
      - name: SQLite (via EF Core)
        kind: database
        purpose: Read-only count queries over the Ticket table
    entities: []

  - name: McpServer
    summary: Lets MCP clients (LLM assistants) use SupportDesk through eight tools that call the existing services.
    behaviour: >
      Runs as a startup mode of the API project (`--mcp`), talking MCP over stdio only; nothing listens
      on the network. Exposes exactly eight tools: create_ticket, list_tickets, get_ticket,
      update_ticket, update_ticket_status, analyze_ticket, draft_reply and get_dashboard_summary. Each
      tool maps its typed arguments to one call on the same service method the REST controller uses
      (Tickets or Dashboard) and maps the result to structured JSON with the REST field names. It holds
      no business rules, no data access and no AI calls of its own, so validation, persistence and AI
      safety come from the services unchanged. Failures (validation, not found, AI not configured,
      timeout, provider error, invalid output, unexpected) become tool error results with the same safe
      title, detail and status as the REST ProblemDetails; never stack traces or secrets. Writes only
      MCP messages to standard output; logs go to standard error. No delete tool. draft_reply returns a
      draft and sends nothing.
    responsibilities:
      - MCP stdio server and tool registration (FR13, FR14.1)
      - Mapping tool arguments to service calls and results to structured output (FR14.2, FR14.3)
      - Mapping typed failures to safe tool error results (FR14.4, FR15.3)
    depends_on:
      - component: Tickets
        interaction: Calls the same ticket service methods as the REST controller for seven tools
        style: sync
      - component: Dashboard
        interaction: Calls the dashboard summary for get_dashboard_summary
        style: sync
    dependents: []
    external_dependencies:
      - name: MCP client (for example Claude Code)
        kind: other
        purpose: Starts the server as a local process and calls its tools over stdio
    entities: []
```

## Component Diagram

```mermaid
graph LR
    Tickets -->|"analyze / draft request (validated result back)"| AiAssistant
    Dashboard -->|"read-only ticket counts"| Tickets
    McpServer -->|"same service calls as REST (7 tools)"| Tickets
    McpServer -->|"summary counts"| Dashboard
```

<!-- Text fallback: Tickets calls AiAssistant to analyze tickets and draft replies and receives validated results. Dashboard reads ticket data owned by Tickets, read-only, to compute counts. McpServer calls the same Tickets service methods as the REST controllers for seven tools and the Dashboard summary for the eighth. AiAssistant depends on no other component; nothing depends on McpServer. -->

Shared plumbing that is not a building block (it owns no business data or rules): the HTTP controllers for each component, the global exception handler that maps typed failures to ProblemDetails (ADR-005), Swagger, configuration binding, and the EF Core database context.

## Component Summary

| Component | Purpose | Depends On | Dependents | Entities Owned |
|---|---|---|---|---|
| Tickets | Ticket lifecycle, filtering, analyze/draft orchestration, only writer of ticket data | AiAssistant | Dashboard | Ticket |
| AiAssistant | AI provider access (Groq), prompts, AI-output validation, timeouts | — | Tickets | — |
| Dashboard | Summary counts | Tickets (read-only data) | McpServer | — |
| McpServer | MCP stdio server with eight tools that call the existing services | Tickets, Dashboard | — | — |

## Entity Ownership

| Entity | Owning Component | Identifier | Attributes | References |
|---|---|---|---|---|
| Ticket | Tickets | Id | Id, Title, Description, Status, Priority, Category, Sentiment, AiSummary, SuggestedAction, DraftReply, DraftReplyGeneratedAt, CreatedAt, UpdatedAt | — |

AI analysis results and drafts are transient values passed from AiAssistant to Tickets; they are stored only as fields of Ticket. Types, limits and allowed values are specified in Functional Design.

## External Dependencies

| Component | Dependency | Kind | Purpose |
|---|---|---|---|
| Tickets | SQLite (via EF Core) | database | Persist tickets across restarts |
| AiAssistant | Groq API | third-party-api | Real AI analysis and drafts when configured |
| Dashboard | SQLite (via EF Core) | database | Read-only count queries |
| McpServer | MCP client (e.g. Claude Code) | other | Starts the server and calls tools over stdio |

## Rationale

| Component | Why it is a separate building block |
|---|---|
| Tickets | Distinct data ownership: the only component that stores and changes tickets, so every ticket rule (validation, status, AI result application, UpdatedAt) lives in one place. |
| AiAssistant | Distinct concern and change rate: provider, prompts and validation change independently of ticket rules; isolating untrusted AI output behind one boundary enforces the "never save unvalidated AI output" rule and lets tests replace the AI with a fake. |
| Dashboard | Distinct reader with no writes and no rules about tickets' lifecycle; keeping counting out of Tickets keeps Tickets focused and lets the dashboard work whatever the AI state. |
| McpServer | Distinct entry point and protocol (MCP over stdio) with its own failure format (tool error results); like the REST controllers it owns no data or rules, but it is a separate block because it is a second, independently started way into the system with its own safety rules (FR15). See ADR-006. |

**Alternatives rejected** (see ADR-001): a fourth TicketAiWorkflow block (extra layer for a two-step flow, against the "no unnecessary layers" practice) and a two-block split folding Dashboard into Tickets (mixes reporting with lifecycle rules).
