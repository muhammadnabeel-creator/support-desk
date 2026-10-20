<!-- INVARIANT: examples are single-line HTML comments so a fresh template parses to total=0 (MEMORY_EMPTY). Do NOT un-comment or split across lines. t100 guards this. -->
> This file is kept up to date automatically while the stage runs. Add observations at the review step, not by editing here directly.

## Interpretations
<!-- example: 2026-05-29T10:14:32Z — chose REST over GraphQL; the consuming team only needs CRUD, revisit if subscriptions land -->
- 2026-10-19T14:36:59Z — Read "grok not xAI" as Groq (groq.com, OpenAI-compatible API) and confirmed it with a follow-up question rather than guessing; Grok (xAI) and Groq are easy to confuse and change the base URL and SDK.

## Deviations
<!-- example: 2026-05-29T10:14:32Z — skipped the optional caching layer the stage prose suggested; the dataset is small enough that it adds risk -->
- 2026-10-19T14:36:59Z — Added fields beyond the brief's suggested data model (DraftReply, DraftReplyGeneratedAt) because the user chose to store the latest draft; added 502/503/504 status mapping for AI failures, which the brief describes only as "understandable error responses".

## Tradeoffs
<!-- example: 2026-05-29T10:14:32Z — picked TDD over BDD this run; the team is unit-first and the domain is well-understood -->
- 2026-10-19T14:36:59Z — Settled small corners (length limits, dashboard "high-priority" = High or Critical, no delete endpoint, no AI retries) as correctable assumptions instead of more questions, following the corner-checklist guidance to ask only what the agent cannot know.

## Open questions
<!-- example: 2026-05-29T10:14:32Z — confirm the retention window with compliance before the next stage hardens the schema -->
