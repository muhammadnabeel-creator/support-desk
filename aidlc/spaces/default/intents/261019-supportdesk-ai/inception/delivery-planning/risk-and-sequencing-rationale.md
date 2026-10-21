# Risk and Sequencing Rationale — SupportDesk AI

A **Bolt** is one build pass over a piece of the work that ends in something that runs and is tested (`bolt-plan.md`). A **walking skeleton** is the smallest version that runs the whole way through, built first to prove the pieces connect.

## Heuristic Used

**Walking-skeleton first, then risk-first** (Cockburn's walking skeleton; Boehm's risk-driven spiral). Chosen in Q1. A formal WSJF score (value + urgency + risk reduction divided by size) was offered and not chosen; with four units and a fixed deadline the ranking is clear without arithmetic.

## Why This Order

1. **U1 first (forced by the graph and chosen as the slice).** Every other unit needs U1's project, database, Ticket entity, shared types and error handler (`unit-of-work-dependency.md`). Its first commits are the thin slice from team practices, which also tackles the "EF Core / SQLite" worry on Day 1.
2. **U2 next (risk-first).** The AI work has the most unknowns (Groq's real response format, validation edge cases, timeouts) and is the most-graded part of the brief. Doing it on Day 3 leaves Day 4–5 to recover if it slips. Topologically U3 could come first; we deviate from "easiest first" on purpose.
3. **U3 then (low risk, small).** One read-only endpoint; a safe task after the hardest one.
4. **U5 MCP Server next (forced by the graph; added 2026-10-21).** Its tools call U1–U3 services, including the dashboard service, so it follows U3. It comes before U4 because the README must document the MCP setup and U4 stores the MCP evidence.
5. **U4 last (forced by the graph).** Documentation and the clean-clone check must describe the finished endpoints and MCP server.
6. **Task F (deferred tests) before the release gate.** The person asked for tests to be written at the end; they must pass, at 80% coverage, before the clean-clone check.

No ordering deviates from the dependency graph's constraints.

## Risk Register

| Risk | Likelihood | Impact | Mitigation | Bolt |
|---|---|---|---|---|
| Groq output doesn't match our expected JSON (wrong field names, extra text, display-form enums) | Medium | High | `response_format: json_object`, tolerant enum matching, strict validator with tests for every bad case; manual Groq check in Bolt 2's Definition of Done | 2 |
| Groq key or service unavailable on demo day | Low | Medium | Mock is the default and is clearly labelled; demo falls back to mock and shows the Groq adapter code | 2, 4 |
| EF Core migrations / SQLite setup trouble (tools missing, file path issues) | Medium | Medium | Done first in Bolt 1; `dotnet-ef` tool install documented in README; tests use SQLite in-memory | 1 |
| Running out of time within 5 days | Medium | High | Riskiest work by Day 3; U3 is small; Day 5 is buffer + release gate; Should-Have US4.3 (`.http` examples) can shrink to README examples if needed | all |
| Not being able to explain the code in the walkthrough | Medium | High | You approve each code plan, read code and tests before merging, and the stories double as a demo script; ADRs in `decisions.md` explain the "why" | all |
| A secret is committed by accident | Low | High | `.gitignore` in Bolt 1, key only in user secrets, secrets check in Bolt 4 | 1, 4 |
| MCP added late (change request on Day 3) squeezes the schedule | High | High | U3 is small and done first; U5 reuses the services so it is mostly mapping code; README MCP section and evidence move into U4; Should-Have US4.3 can shrink to README examples | 4, 5 |
| Deferred tests (Task F) left for Day 5 miss the 80% floor | Medium | High | Test lists per unit are already written (unit-test-instructions.md); Task F starts as soon as U4 code is done; the floor is never lowered | F |
| MCP stdout polluted by logs or build output, breaking the client | Medium | Medium | Logs to standard error only; client launches the built dll, not `dotnet run`; a stdout check in U5 tests (AC5.1.4) | 4 |
| Coverage below the 80% floor | Low | Medium | Tests written per layer as each unit is built (test-after per layer), measured with coverlet before each merge | all |

## Worries Raised (Q5) and Where They Are Tackled

- Groq + AI validation -> Bolt 2, placed second on purpose.
- EF Core / SQLite -> Bolt 1, Day 1.
- 5-day deadline -> order above plus Day 5 buffer.
- Walkthrough -> plan approval and code reading in every Bolt (`team-allocation.md`).
