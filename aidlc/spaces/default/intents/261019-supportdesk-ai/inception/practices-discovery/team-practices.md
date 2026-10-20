# Team Practices — SupportDesk AI

Affirmed in the practices-discovery interview (see
`practices-discovery-questions.md`). One developer, ASP.NET Core 8 / C#, runs on a
local machine only, no CI service, a 5-day delivery window, and a required GitHub
push with meaningful commits.

## Way of Working

Our trunk is `main`. The local repository was initialised on `master`, and we
rename it to `main` (`git branch -m master main`) before the first commit.

Each unit of work gets its own short-lived branch, created from `main` and merged
back into `main` when the unit is finished, usually on the same day and never
later than the next. We merge with a merge commit (`git merge --no-ff`) and never
squash, so the unit's individual commits stay visible in the GitHub history for
the reviewer. When Construction runs a Unit in a Bolt worktree, the worktree flags
are `--base main --target main --strategy merge`.

Commits are small and self-contained, and each one builds. Messages use
Conventional Commits prefixes with a short imperative subject: `feat:`, `fix:`,
`test:`, `docs:`, `refactor:`, `chore:` (for example
`feat: add create-ticket endpoint`).

We push to GitHub at least once per working day, and the reviewer has access to
the repository before submission. We commit the whole workspace: source, tests,
README, the AI-DLC records (`aidlc/`), the AI-DLC setup (`.claude/`) and
`docs/requirements.pdf`. Local-only files stay excluded by the existing AI-DLC
`.gitignore` block.

## Walking Skeleton

We build a thin end-to-end slice first, without a formal skeleton checkpoint
ceremony; this matches the `classic` scope's `skeleton: off` setting. The first
unit of work is that slice:

- project creation, EF Core with SQLite, and an initial migration;
- `POST /api/tickets`, `GET /api/tickets` and `GET /api/tickets/{id}`, running
  through controller, service and database;
- Swagger;
- the standard .NET `.gitignore` entries (see Code Style).

The slice counts as done when a created ticket survives an application restart.
The AI integration, dashboard and filtering come in later units.

Every unit, the first one included, is verified with
`dotnet build && dotnet test` run from the repository root. That command is the
candidate Construction Verification Command; it is recorded when it is authorised
at Construction.

## Testing Posture

- **Methodology**: test-after
- **Methodology evidence**: The trainee chose it in the interview (Q4, option A).
  It suits a solo developer learning C# and ASP.NET Core under a 5-day deadline,
  and the brief asks for tests of the key business rules and AI-output
  validation without requiring tests to be written first.
- **Ordering**: For each unit, we implement each testable layer (DTO validation,
  then services, then controllers) and then write and run that layer's tests
  before we move to the next layer or merge the unit into `main`.
- **Framework**: xUnit with the built-in `Assert`, in a separate test project in
  the solution. We do not use extra assertion or mocking libraries.
- **Fakes**: hand-written fakes for `IAiTicketService` and other interfaces.
  Automated tests never call a real AI provider. Canned fake responses cover:
  - valid output;
  - malformed JSON;
  - out-of-range enum values;
  - a timeout;
  - provider errors.
- **Database tests**: service tests use EF Core against SQLite in-memory mode
  (`DataSource=:memory:` with a connection held open), not the EF Core InMemory
  provider.
- **API tests**: a few `WebApplicationFactory` tests cover the main endpoints.
  Each test replaces `IAiTicketService` with a fake and uses its own SQLite
  in-memory database.
- **Behaviour we always cover**:
  - ticket validation (required fields, length limits);
  - status values (only Open, InProgress, Resolved and Closed are accepted);
  - 404 for unknown IDs;
  - AI-output validation: typed DTO and allowed values, and malformed output is
    not saved;
  - AI timeouts and provider failures return a controlled error;
  - draft-only replies;
  - dashboard counts.
- **Coverage floor**: at least 80% line coverage, measured with coverlet
  (`dotnet test --collect:"XPlat Code Coverage"`) on hand-written code only.
  EF Core migrations and `Program.cs` are excluded from measurement. We do not
  lower this floor to make a step pass.
- **Checks before merge (in place of CI)**: there is no CI service. Before a unit
  branch is merged into `main` and before every push, `dotnet build` and
  `dotnet test` must pass locally, and `dotnet format --verify-no-changes` must
  report no changes. We never merge or push a failing build.
- **Test strategy**: Standard (from the scope). The notes above add to it and
  never replace it.

## Deployment

We have no deployed environments. The brief puts production deployment out of
scope, and the reviewer clones the repository and runs it locally.

- **Run target**: a developer machine with the .NET 8 SDK. The API starts with
  `dotnet run`, and Swagger UI is enabled in the Development environment.
- **Database**: the SQLite database is created by a documented
  `dotnet ef database update` step in the README. Migrations are not applied
  automatically at startup, and the database file is never committed.
- **Configuration**: AI provider settings (endpoint, model, key, timeout) come
  from environment variables or `dotnet user-secrets`, which are stored outside
  the repository. Committed files (`appsettings.json`, `.env.example`) hold
  placeholder values only. A clearly named mock AI implementation can be selected
  through configuration for development.
- **Release**: a release is a push of `main` to GitHub. The submitted commit is
  tagged (for example `v1.0-submission`).
- **Release gate**: before submission, we clone the repository fresh into an
  empty folder, follow the README step by step, run the tests, start the API and
  exercise the endpoints through Swagger.
- **Rollback**: we `git revert` the faulty commit or go back to the previous tag.
  For the database, we delete the local SQLite file and run
  `dotnet ef database update` again. Once a migration is committed, we never
  edit it.

## Code Style

- **Formatter**: `dotnet format`, configured by a root `.editorconfig` that
  follows the standard .NET/C# conventions.
- **Analyzers**: the built-in .NET analyzers are on, and every project sets
  `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`.
  Analyzer warnings are shown but are not treated as errors; we fix them as we go.
- **Naming**:
  - PascalCase for types, methods, properties, constants and enum members;
  - camelCase for parameters and locals;
  - `_camelCase` for private fields;
  - an `I` prefix on interfaces (for example `IAiTicketService`);
  - an `Async` suffix on async methods;
  - file-scoped namespaces, one type per file, with the file named after the
    type.
- **Structure**:
  - Controllers stay thin and only map HTTP to service calls.
  - Business logic lives in services registered through DI.
  - Request and response DTOs are separate from EF entities.
  - Fixed values (status, priority) are enums.
  - We use `async`/`await` and pass a `CancellationToken` through to EF Core and
    the AI client.
  - We do not add architecture layers the brief does not need.
- **Repository hygiene**: as part of the first unit of work, `.gitignore` gains
  the standard .NET entries (`bin/`, `obj/`, `*.user`, `.vs/`, `TestResults/`,
  `*.db`, `*.db-shm`, `*.db-wal`, `.env`). User secrets already live outside the
  repository, in the user profile, so they need no entry.
