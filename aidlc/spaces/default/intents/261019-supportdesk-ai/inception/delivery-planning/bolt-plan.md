# Bolt Plan — SupportDesk AI

A **Bolt** is one build pass over a piece of the work that ends in something that runs and is tested. Here each Bolt is exactly one unit of work from `inception/units-generation/unit-of-work.md`, built on its own short-lived branch and merged into `main` with `git merge --no-ff` (team practices: `inception/practices-discovery/team-practices.md`). Bolts run one after another (Q3). Order and reasons: `risk-and-sequencing-rationale.md`.

Every Bolt's Definition of Done also includes the team's standing checks: `dotnet build` and `dotnet test` pass, `dotnet format --verify-no-changes` reports nothing, Conventional Commit messages, merged to `main`, pushed to GitHub.

Sources: requirements (`inception/requirements-analysis/requirements.md`), stories (`inception/user-stories/stories.md`), components (`inception/domain-design/components.md`), units, dependency graph and story map (`inception/units-generation/unit-of-work.md`, `unit-of-work-dependency.md`, `unit-of-work-story-map.md`), contracts (`inception/contract-design/contract-summary.md`), team practices (`inception/practices-discovery/team-practices.md`).

The order respects the dependency graph in `unit-of-work-dependency.md` (U1 first; U2 and U3 independent; U5 after U1–U3; U4 last) and uses the contracts in `inception/contract-design/contract-summary.md`.

## Sequence at a Glance

| # | Bolt | Unit | Branch | Target day | Stories |
|---|---|---|---|---|---|
| 1 | Foundation & Tickets | U1 `u1-foundation-tickets` | `unit/u1-foundation-tickets` | Day 1–2 | US1.1–US1.5, US4.1 |
| 2 | AI Assistant | U2 `u2-ai-assistant` | `unit/u2-ai-assistant` | Day 3 | US2.1–US2.5 |
| 3 | Dashboard | U3 `u3-dashboard` | `unit/u3-dashboard` | Day 3 (2026-10-21) | US3.1 |
| 4 | MCP Server | U5 `u5-mcp-server` | `unit/u5-mcp-server` | Day 3 (afternoon) – Day 4 (2026-10-22) | US5.1–US5.4 |
| 5 | Release Kit | U4 `u4-release-kit` | `unit/u4-release-kit` | Day 4 (afternoon) – Day 5 | US4.2–US4.4, US5.5 |
| — | Task F — deferred tests | all units | on each unit branch or one test branch | Day 5 (before the release gate) | automated tests for every unit, 80% coverage |

**Re-planned 2026-10-21 (MCP change request).** Bolts 1 and 2 are done and merged. The trainer's updated brief adds the MCP server (U5), which goes after the Dashboard because its `get_dashboard_summary` tool needs the dashboard service, and before the Release Kit because the README must document the MCP setup and hold the MCP evidence. Day 5 holds the deferred automated tests (Task F, which the person asked to write at the end), the clean-clone release gate, the submission tag and demo/walkthrough preparation; there is no other buffer left.

## Bolt 1 — Foundation & Tickets (U1)

- **Units**: U1.
- **Thin slice first** (walking skeleton: the smallest version that runs the whole way through — controller, service, SQLite). The classic workflow runs no separate skeleton checkpoint, but U1's first commits are this slice: create, list and get a ticket, surviving an app restart.
- **Definition of Done**:
  - Solution, Web API project, xUnit test project, `.editorconfig`, .NET `.gitignore` entries.
  - EF Core + SQLite with the initial migration; database created by `dotnet ef database update`.
  - Shared enums, the five typed exceptions and the global ProblemDetails handler (contract C3).
  - All five ticket endpoints per contract C1, with validation; Swagger lists them.
  - Tests for US1.1–US1.5 and US4.1's acceptance criteria (validation, 404, 500 without stack trace, malformed body); coverage on track for 80%.
- **Confidence hypothesis**: the controller -> service -> EF Core -> SQLite path works end to end and data survives a restart; the test setup (SQLite in-memory, `WebApplicationFactory`) is solid enough to reuse in every later Bolt.
- **Expected demo**: in Swagger, create a ticket, restart the app, get it back; show a 400 for a blank title and a 404 for an unknown id.

## Bolt 2 — AI Assistant (U2)

- **Units**: U2.
- **Definition of Done**:
  - `IAiTicketService` (contract C2) and `GroqAiTicketService` (contract C4). (As built 2026-10-20: Groq only, no mock provider; default model `openai/gpt-oss-120b`.)
  - Shared validator; timeout (default 20 s) and cancellation; typed failures mapped to 502/503/504.
  - `POST /api/tickets/{id}/analyze` (overwrites priority, returns `previousPriority`) and `POST /api/tickets/{id}/draft-reply` (stored, never sent).
  - Tests with fakes for valid output, malformed JSON, invalid enum, missing/over-long fields, timeout, provider error, not configured; tests never call Groq.
  - Manual check with your real Groq key (from user secrets): one analyze and one draft on demo data.
- **Confidence hypothesis**: Groq's real responses pass our validator for normal tickets, and every bad-output case is refused without changing the ticket — the riskiest and most-graded behaviour works.
- **Expected demo**: analyze a ticket with the mock, then with Groq; show a refused bad output (via test) and a 503 when the key is missing.

## Bolt 3 — Dashboard (U3)

- **Units**: U3.
- **Definition of Done**: `GET /api/dashboard/summary` per contract C1 with read-only queries; tests for empty data and mixed data (AC3.1.1–AC3.1.3); works with the AI unconfigured.
- **Confidence hypothesis**: counts computed in the database match the data, including `highPriority` (High or Critical) and `unanalyzed`.
- **Expected demo**: create and analyze a few tickets, then show the summary counts.

## Bolt 4 — MCP Server (U5) (added 2026-10-21)

- **Units**: U5.
- **Definition of Done**:
  - `ModelContextProtocol` package; `--mcp` startup mode with stdio transport, logs to standard error only, content root at the API project folder, user secrets loaded explicitly (ADR-006).
  - Service registration moved into one shared method; existing REST behaviour and tests unchanged (contract C6).
  - The eight tools per contract C5, each calling the same service method as its REST endpoint and reusing the REST response builders; shared id check and enum parsing; failures as `isError` results with REST status, title and detail.
  - Manual check with Claude Code: tools listed, `create_ticket`, `list_tickets`, `analyze_ticket` and `draft_reply` succeed on demo data; screenshot or log saved for U4.
- **Confidence hypothesis**: an MCP client launched from any folder sees exactly eight tools, uses the same database and Groq key as the API, and gets the same results and safe errors as the REST endpoints.
- **Expected demo**: in Claude Code, ask to list tickets, analyze one and draft a reply; then show the same ticket in Swagger.

## Bolt 5 — Release Kit (U4)

- **Units**: U4.
- **Definition of Done**: README (prerequisites, setup, configuration with user-secrets example, `dotnet ef database update`, endpoint examples, MCP server setup with sample Claude Code and Claude Desktop configurations and MCP Inspector, known limitations, demo-data-only warning for Groq and MCP clients); MCP evidence in `docs/mcp-evidence/` (US5.5); `.http` file with a request per endpoint plus an invalid one; placeholder-only `appsettings.json` and `.env.example`; secrets check; clean-clone release gate passed (US4.2); submitted commit tagged.
- **Confidence hypothesis**: a reviewer with only the .NET 8 SDK and no AI key can clone, set up, run and test the project from the README alone.
- **Expected demo**: the clean-clone run itself, start to finish.
