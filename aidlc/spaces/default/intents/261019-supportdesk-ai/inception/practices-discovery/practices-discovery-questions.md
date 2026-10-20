# Practices Discovery — Questions

These questions settle how you will work on SupportDesk AI. Each one starts from a suggested default (marked "(Suggested)") adapted to a solo, 5-day, local-only .NET project. Draft details: `team-practices.md`, `discovered-rules.md`, `evidence.md` in this folder.

## Way of Working

### Q1. Branching and commits
Your trainer wants "meaningful commits" on GitHub. How should work land in the repository? (The repo is currently on a `master` branch with no commits.)

A. (Suggested) Rename `master` to `main`; commit straight to `main` in small commits, one or more per unit of work, using Conventional Commits prefixes (`feat:`, `fix:`, `test:`, `docs:`, `chore:`)
B. Same as A, but use a short-lived branch per unit of work and merge it into `main` (keeping its commits visible)
C. Keep `master`; commit straight to it with free-form messages
X. Other (please specify)

[Answer]: B

### Q2. What goes into the GitHub repository
Besides your source code and README, which of these should be committed?

A. (Suggested) Everything: the AI-DLC records (`aidlc/`), the AI-DLC setup (`.claude/`), and `docs/requirements.pdf` — the reviewer sees how the project was planned and can re-run the workflow
B. Commit `aidlc/` and `docs/`, but keep `.claude/` out of Git
C. Only source code, tests and README; keep all AI-DLC files local
X. Other (please specify)

[Answer]: A

## Walking Skeleton

### Q3. Build a thin end-to-end slice first?
A walking skeleton is a minimal version that runs the whole way through, built first to prove the pieces connect before the real features go in. Here that would be: create/list/get a ticket through controller -> service -> SQLite, surviving an app restart.

A. (Suggested) Yes, plan it as the first unit of work (matches the brief's Day 1), but without an extra formal checkpoint ceremony; verify each unit with `dotnet build && dotnet test`
B. Yes, and add a formal checkpoint: I run it end to end and approve it before any other unit starts
C. No, build units in whatever order the plan suggests
X. Other (please specify)

[Answer]: A

## Testing Posture

### Q4. When are tests written?
A. (Suggested) Test-after: implement a layer (e.g. the ticket service), then write and run its tests
B. Tests first (TDD) for validation and business rules (status values, AI-output validation); test-after for everything else
C. Tests first (TDD) for everything
X. Other (please specify)

[Answer]: A

### Q5. Test tooling and coverage
The default AI-DLC rule for this workflow is an 80% line-coverage floor. Which setup do you want?

A. (Suggested) xUnit + built-in Assert, hand-written fakes for `IAiTicketService` (tests never call a real AI), SQLite in-memory for database tests, a few `WebApplicationFactory` API tests; 80% line coverage measured with coverlet on hand-written code (migrations and `Program.cs` excluded)
B. Same as A, plus FluentAssertions/Shouldly and NSubstitute for mocks
C. Same as A, but no coverage floor — just tests for key business rules and AI validation as the brief requires
X. Other (please specify)

[Answer]: A

## Deployment

### Q6. How is the project "released"?
There is no server; your reviewer clones and runs it locally.

A. (Suggested) Release = push to GitHub; before submitting, do a fresh clone and follow the README step by step; tag the submitted commit (e.g. `v1.0-submission`); database created with a documented `dotnet ef database update` step
B. Same as A, but apply EF Core migrations automatically when the app starts (fewer setup steps for the reviewer)
C. Same as A, plus a GitHub Actions workflow that runs build and tests on every push (stretch goal)
X. Other (please specify)

[Answer]: A

## Code Style

### Q7. Formatting and code checks
A. (Suggested) `.editorconfig` + `dotnet format` + built-in .NET analyzers, nullable reference types on, standard C# naming (PascalCase types/methods, `_camelCase` private fields, `Async` suffix on async methods); warnings shown but not errors
B. Same as A, but treat warnings as errors (build fails on any warning)
C. No formatter or analyzer setup; just follow the default C# style
X. Other (please specify)

[Answer]: A

## Hard Rules

### Q8. Which of these are non-negotiable rules for this project? (select all that apply)
These come from the requirements brief. Rules you pick are saved as permanent "always/never" rules every later stage must follow; the rest stay as good practice.

A. NEVER commit API keys, passwords or other secrets (config comes from env vars / user secrets; only placeholder examples committed)
B. NEVER send an AI-generated reply automatically — drafts are returned for human review only
C. NEVER save or return AI output that has not been validated against a typed DTO and allowed values (status/priority enums)
D. NEVER send real customer, confidential or personal data to an external AI provider; NEVER execute AI-generated code, SQL or commands
E. ALWAYS keep core ticket operations working when the AI provider is unavailable, with an AI request timeout and no stack traces or secrets in error responses
X. Other (please specify)

[Answer]: A, B, C, D, E
