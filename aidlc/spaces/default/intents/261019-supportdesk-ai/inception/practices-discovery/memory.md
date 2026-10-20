<!-- INVARIANT: examples are single-line HTML comments so a fresh template parses to total=0 (MEMORY_EMPTY). Do NOT un-comment or split across lines. t100 guards this. -->
> This file is kept up to date automatically while the stage runs. Add observations at the review step, not by editing here directly.

## Interpretations
<!-- example: 2026-05-29T10:14:32Z — chose REST over GraphQL; the consuming team only needs CRUD, revisit if subscriptions land -->
- 2026-10-19T14:29:32Z — Treated org.md defaults as suggestions adapted to a solo, 5-day, local-only .NET trainee project; replaced Prettier/ESLint examples with .editorconfig + dotnet format, and "deploy on merge to staging" with push-to-GitHub plus a clean-clone check, since there is no server or CI platform.

## Deviations
<!-- example: 2026-05-29T10:14:32Z — skipped the optional caching layer the stage prose suggested; the dataset is small enough that it adds risk -->
- 2026-10-19T14:29:32Z — Unit branches merge into main keeping their commits instead of the org default squash-merge; the trainer's brief explicitly grades "meaningful commits", which squashing would hide.

## Tradeoffs
<!-- example: 2026-05-29T10:14:32Z — picked TDD over BDD this run; the team is unit-first and the domain is well-understood -->
- 2026-10-19T14:29:32Z — Kept the 80% line-coverage floor (org default for classic scope) but measured on hand-written code only, excluding EF migrations and Program.cs; a local build + test + format check stands in for "CI before merge" because no CI is in scope.

## Open questions
<!-- example: 2026-05-29T10:14:32Z — confirm the retention window with compliance before the next stage hardens the schema -->
- 2026-10-19T14:29:32Z — The AI provider choice (hosted LLM vs Ollama, mock as development default) is not a practices question; settle it in Requirements Analysis.
