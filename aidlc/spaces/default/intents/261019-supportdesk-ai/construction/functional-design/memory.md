<!-- INVARIANT: examples are single-line HTML comments so a fresh template parses to total=0 (MEMORY_EMPTY). Do NOT un-comment or split across lines. t100 guards this. -->
> This file is kept up to date automatically while the stage runs. Add observations at the review step, not by editing here directly.

## Interpretations
<!-- example: 2026-05-29T10:14:32Z — chose REST over GraphQL; the consuming team only needs CRUD, revisit if subscriptions land -->
- 2026-10-20T09:45:00Z [unit u1-foundation-tickets] — Asked no new questions for U1: requirements, stories and contracts already settled every corner (limits, ordering, filters, status flow, error codes); the open review points from earlier stages (bad id -> 400 not 404, typed exceptions in U1) were written in as rules BR3.2 and BR6.4.

## Deviations
<!-- example: 2026-05-29T10:14:32Z — skipped the optional caching layer the stage prose suggested; the dataset is small enough that it adds risk -->
- 2026-10-20T12:45:00Z [unit u2-ai-assistant] — Removed the mock AI provider at the person's request ("lets go with groq no mock ai", confirmed); updated FR11, NFR10, US2.5, AC4.2.1 and contract C2 so earlier artifacts match. The reviewer now needs a Groq key for the two AI endpoints; tests still use hand-written fakes.

## Tradeoffs
<!-- example: 2026-05-29T10:14:32Z — picked TDD over BDD this run; the team is unit-first and the domain is well-understood -->

## Open questions
<!-- example: 2026-05-29T10:14:32Z — confirm the retention window with compliance before the next stage hardens the schema -->
