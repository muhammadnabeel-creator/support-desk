# Evidence — Practices Discovery (SupportDesk AI)

This was a lead-only run, so there are no support agents and no contribution
files. The project is greenfield: the five `org.md` sections served as suggested
defaults, the pipeline-deploy lead adapted them in a draft, the human answered
`practices-discovery-questions.md`, and the lead integrated the result.

## Sources Inspected

| Participant | Source | What was read | Result |
|---|---|---|---|
| Lead | `.claude/aidlc-common/stages/inception/practices-discovery.md` | Stage definition | The shapes of the four artifacts and the required `Testing Posture` fields |
| Lead | `aidlc/spaces/default/memory/org.md` | Way of Working, Walking Skeleton, Testing Posture, Deployment, Code Style | The suggested defaults |
| Lead | `aidlc/spaces/default/memory/phases/inception.md` | Inception guardrails | Each rule traces to a section of the brief |
| Lead | `memory/team.md`, `memory/project.md` | Empty templates (per the conductor) | No affirmed baseline existed |
| Lead | `<record>/aidlc-state.md` | Project type and scope settings | Greenfield; `classic` scope; Test Strategy Standard; Construction unit-major, serial; `ci-pipeline` and all Operation stages SKIP |
| Lead | `.claude/scopes/aidlc-classic.md` | Scope frontmatter | `skeleton: off`, `collaborators: off` |
| Lead | Requirements brief (`knowledge/documentkb/01a15482-.../content.md`, `summary.md`) | FR-01..FR-10, §5–§12 | Stack, constraints, 5-day plan, definition of done, candidate rules |
| Lead | `git status --short --branch` | Repository state | `## No commits yet on master` |
| Lead | `.gitignore` | Contents | Only the AI-DLC block; no .NET entries |
| Lead | `dotnet --list-sdks` | Toolchain | `8.0.131` |
| Human | `practices-discovery-questions.md` | Answers to Q1–Q8 | See Interview Decisions |

## Inferences (lead draft)

- **Commit history.** The brief grades "meaningful commits", so the history should
  not be squashed away. The interview confirmed this, with per-unit branches merged
  without squashing.
- **Branch name.** The repository is on `master`, while the defaults assume `main`.
  Renaming costs nothing before the first commit.
- **Walking skeleton.** Day 1 of the brief already describes the thin end-to-end
  slice. Ordering the first unit that way costs nothing, even though the scope
  turns the skeleton ceremony off.
- **No CI service.** Local build, test and format checks before merging and
  pushing stand in for the org's "CI execution before merge".
- **Coverage.** The `classic` scope carries the org's 80% line-coverage floor,
  which cannot be weakened. The only lever is which code is measured.
- **`.gitignore` gap.** It lacks the .NET entries the brief's definition of done
  implies ("excludes secrets and generated build files").

## Org Default Adaptations (affirmed)

| Area | Org default | Affirmed for this project | Why |
|---|---|---|---|
| Way of Working | Trunk-based; squash-merge Bolts into `main` | Trunk `main` (renamed from `master`); one short-lived branch per unit, merged with `--no-ff`; worktree strategy `merge`; Conventional Commits | Solo developer; the reviewer grades commit history |
| Walking Skeleton | Ceremony only when the scope says `skeleton: on` | Ceremony off; first unit is the thin slice; every unit verified with `dotnet build && dotnet test` | Follows the scope setting; matches brief Day 1 |
| Testing Posture | test-after; 80% floor and CI before merge | test-after; 80% coverlet floor on hand-written code (migrations and `Program.cs` excluded); local build, test and format checks before merge and push | No CI service |
| Deployment | Deploy on merge to staging; production manually gated | No environments; release = push plus a tag on the submitted commit; fresh-clone README check; documented `dotnet ef database update` | Local only; production deployment out of scope |
| Code Style | Prettier/ESLint/Black; lint in CI | `.editorconfig`, `dotnet format`, built-in analyzers, nullable on, C# naming; warnings are not errors | .NET stack; no CI |

## Interview Decisions

| Question | Answer | Decision |
|---|---|---|
| Q1 Branching and commits | B | Rename `master` to `main`; one short-lived branch per unit of work, merged into `main` with its commits kept (no squash); Conventional Commits prefixes |
| Q2 What goes into the repository | A | Commit everything: source, tests, README, `aidlc/`, `.claude/` and `docs/requirements.pdf`; local-only files stay ignored by the existing `.gitignore` block |
| Q3 Thin end-to-end slice first | A | Plan the slice (create/list/get through controller, service and SQLite, surviving a restart) as the first unit; no formal skeleton checkpoint; verify each unit with `dotnet build && dotnet test` |
| Q4 When tests are written | A | test-after |
| Q5 Test tooling and coverage | A | xUnit with the built-in `Assert`; hand-written `IAiTicketService` fakes (never a real AI); SQLite in-memory database tests; a few `WebApplicationFactory` API tests; at least 80% line coverage measured with coverlet, excluding migrations and `Program.cs` |
| Q6 Release | A | Release = push to GitHub; fresh-clone README check before submission; tag the submitted commit; database created by a documented `dotnet ef database update` (no migration at startup) |
| Q7 Formatting and checks | A | `.editorconfig`, `dotnet format`, built-in analyzers, nullable on, standard C# naming; warnings are not errors |
| Q8 Hard rules | A, B, C, D, E | All five confirmed as hard constraints; D is split into two NEVER rules in `discovered-rules.md` |

## Draft Candidates Not Adopted as Hard Rules

These rules were drafted as candidates but were not offered or selected in Q8.
Where they still apply, they are kept as practices, not rules:

- ALWAYS call the AI provider through `IAiTicketService` and label mocks clearly.
  Kept as practice in Deployment (Configuration) and Code Style (Structure); it is
  also a brief §7 technical expectation.
- ALWAYS keep the repository runnable from a fresh clone via the README. Kept as
  practice in Deployment (Release gate).
- ALWAYS run build and test locally before pushing. Kept as practice in Testing
  Posture (Checks before merge).
- NEVER commit `bin/`, `obj/` or the SQLite database file. Kept as practice in Code
  Style (Repository hygiene) and Deployment (Database).
- NEVER call a real AI provider from automated tests. Kept as practice in Testing
  Posture (Fakes).
- The separate ALWAYS rules on AI output validation and on timeouts and
  controlled errors were folded into the confirmed rules C and E.

## Remaining Uncertainty

- **AI provider choice.** A hosted LLM or Ollama, chosen by the trainer, is
  deferred to Requirements Analysis. It determines the configuration keys and how
  the "no real personal data to an external provider" rule applies in practice
  (with Ollama, the provider runs locally).
- **API design.** The `ProblemDetails` error shape and serialising enums as
  strings are left to contract design; they are not practices.
- **Verification command.** `dotnet build && dotnet test` is the chosen
  command, but it is recorded only through the Construction verification-command
  checkpoint.
- **Submission tag.** The exact tag name (for example `v1.0-submission`) is
  settled at submission.

## Process Notes

- In the lead draft session, `aidlc engine now` and `aidlc engine knowledge show`
  could not run because the agent's shell started in `.claude/`. The brief was
  read from the catalogued `content.md` and `summary.md` instead. The final
  timestamp (`2026-10-19T14:29:32Z`) was supplied by the conductor.
- The stage diary (`memory.md`) is kept by the conductor and was not edited.
