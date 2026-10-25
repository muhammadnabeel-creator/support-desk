# SupportDesk AI — Progress Log

Each task is ticked only after its approval. Plan: `aidlc/spaces/default/intents/261019-supportdesk-ai/inception/delivery-planning/bolt-plan.md`.

## Planning (Inception) — done 2026-10-19

- [x] Team practices and hard rules
- [x] Requirements (FR1–FR12, NFR1–NFR10)
- [x] User stories (15 stories)
- [x] Domain design (3 components, ADR-001..005)
- [x] Units of work (U1–U4)
- [x] API contracts (C1–C4)
- [x] Delivery plan (5-day Bolt order)

## Unit 1 — Foundation & Tickets

- [x] Functional design (entities, 22 rules, 6 workflows) — reviewed READY, approved 2026-10-20
- [x] Non-functional requirements (performance, security, reliability, logging, tech stack) — reviewed READY, approved 2026-10-20
- [x] Non-functional design (security layers, fixed DB path, logging, components) — reviewed READY, approved 2026-10-20
- [x] Code plan (approved 2026-10-20; built in 5 tasks A–E, each approved by you)
- [x] Task A — repo setup, solution, API project, test runner (2 smoke tests green; committed by you)
- [x] Task B — data model and database (Ticket, enums, DbContext, fixed DB path, first migration) — approved 2026-10-20 (3 early test files kept uncommitted for Task F)
- [x] Task C — error handling and ticket business logic — approved 2026-10-20
- [x] Task D — API endpoints (tests deferred to F) — smoke-checked with curl, approved 2026-10-20
- [x] Task E — configuration, docs, format — approved 2026-10-20
- [ ] Task F — tests (deferred; only when you say) and the 80% coverage check
- [x] `dotnet build && dotnet test` passes (18/18); code review READY; manually tested by you; Unit 1 signed off and merged to `main` 2026-10-20

## Unit 2 — AI Assistant (Groq only, no mock — decided 2026-10-20)

- [x] Functional design (22 rules, analyze/draft flows, failure map) — reviewed READY, approved 2026-10-20
- [x] Non-functional requirements and design (timeouts, secrets, untrusted output, logging) — reviewed READY, approved 2026-10-20
- [x] Code plan (approved 2026-10-20; 4 build tasks A–D, tests in Task F)
- [x] Task A — AI building blocks (interface, results, options, prompts, output validator, Groq message types) — approved 2026-10-20
- [x] Task B — Groq client, timeout, error mapping, registration and config — approved 2026-10-20
- [x] Task C — analyze and draft-reply in the ticket service and the two endpoints — approved 2026-10-20
- [x] Task D — no-key check (503), user-secrets commands, docs and format — approved 2026-10-20
- [x] Code review READY (5 minor findings; R-01 key trim and R-02 token limits fixed, the rest carried to U4); `dotnet build && dotnet test` verified 18/18
- [x] Groq model switched to `openai/gpt-oss-120b` (old model returned 404); analyze and draft-reply tested with your key; Unit 2 signed off 2026-10-20
- [ ] Unit 2 merged to `main` (by you)

## Change Request — MCP server (trainer's updated brief, 2026-10-21)

- [x] Requirements FR13–FR15, NFR11–NFR12 (stdio only, Claude Code as client, tool error results) — reviewed READY, approved
- [x] User stories US5.1–US5.5 — reviewed, gaps fixed, approved
- [x] Domain design: McpServer component, ADR-006 (`--mcp` mode in the API project, tools call the same services) — approved
- [x] Units: new U5 MCP Server — approved
- [x] Contracts C5 (MCP tools) and C6 (shared services) — approved
- [x] Delivery plan: U3 → U5 → U4 → Task F tests — approved

## Unit 3 — Dashboard

- [x] Implemented directly at your request (2026-10-21): `GET /api/dashboard/summary` with `IDashboardService` (read-only counts, no AI); build 0 warnings, 18/18 tests, format clean; smoke-checked against the ticket data
- [ ] Your Postman check; AI-DLC records for U3 to be caught up; merged to `main`

## Unit 5 — MCP Server (built before Unit 4)

- [x] Implemented directly (2026-10-21): `--mcp` stdio mode with the official SDK (ModelContextProtocol 2.3.0), 8 tools calling the same services, shared registration/JSON/error mapping/input parsing; build 0 warnings, 18/18 tests, format clean
- [x] Scripted stdio check from another folder: 8 tools listed, all tools OK, 400/404 errors in the shared shape, analyze + draft via Groq OK, stdout clean
- [ ] Your Claude Code check + evidence screenshot/log; merged to `main`

## Unit 4 — Release Kit

- [x] README (setup, user secrets, database step, endpoints + real examples, errors, MCP setup for Claude Code / Claude Desktop / MCP Inspector, tests, structure, design notes, known limitations) — 2026-10-21
- [x] `src/SupportDesk.Api/SupportDesk.Api.http` (every endpoint + invalid examples), `.env.example` (placeholders only)
- [x] `docs/mcp-evidence/` with the stdio smoke-test log; ProblemDetails now also for unknown routes (404) and wrong methods (405)
- [x] Secrets scan clean; database file ignored by Git
- [ ] Your Claude Code screenshot/log in `docs/mcp-evidence/`; merged to `main`
- [ ] Clean-clone release gate (after Task F tests)
