# User Stories Assessment — SupportDesk AI

## Decision

**Execute.**

## Rationale

SupportDesk AI is a user-facing API: a support agent creates, reviews, updates and closes tickets, and asks the AI for analysis and reply drafts. The behaviour that matters most to the reviewer (validated AI output, draft-only replies, the API working when the AI is down) is business logic that is easiest to verify as Given/When/Then acceptance criteria tied to a person's goal. Stories also give the trainee a walkthrough script: each story is one demo step.

## Factors Considered

- **Project type**: greenfield Web API with Swagger as the interface (no UI), so stories describe API interactions, not screens.
- **User-facing scope**: all twelve requirement groups in `requirements.md` (FR1–FR12) are actions a person takes or sees results of.
- **Complexity signals**: AI-output validation, priority overwrite on analysis (FR7.3), stored-but-never-sent drafts (FR8), and error mapping (FR10) carry rules worth expressing as testable criteria.
- **Personas**: few (a support agent, plus a lead reading the dashboard, plus the reviewer running the project) — a light persona set is enough.

## Where Stories Add the Most Value

- AI analysis and draft reply: the happy path, invalid AI output, timeout, provider not configured.
- Ticket lifecycle: create -> list/filter -> update -> status -> close, including 400/404 paths.
- Running the project from a clean clone with the mock provider (the brief's definition of done).
- MCP access (change request 2026-10-21, FR13–FR15): an MCP client lists exactly eight tools, each tool behaves like its REST endpoint, unsafe input becomes a safe error result, and the setup is documented with evidence. Stories make the "same service, no duplicated logic" rule testable per tool.
