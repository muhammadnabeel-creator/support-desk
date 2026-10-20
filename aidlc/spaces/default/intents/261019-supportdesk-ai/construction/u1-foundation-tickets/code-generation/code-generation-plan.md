# Code Generation Plan — U1 Foundation & Tickets

## Summary

- Builds: the .NET 8 solution, SQLite + EF Core with the first migration, shared enums and error handling, and all 5 ticket endpoints (create, list/filter, get, update, status) with Swagger
- Touches: `SupportDesk.sln`, `src/SupportDesk.Api/` (Domain, Tickets, Infrastructure, Errors, Program.cs), `tests/SupportDesk.Api.Tests/`, `.gitignore`, `.editorconfig`
- Tests: about 40 xUnit tests — data (5), service (15), error handling (5), API end-to-end with WebApplicationFactory (15), all tagged `Unit=U1`

## Inputs

Design for this unit: `construction/u1-foundation-tickets/functional-design/` (entities.md, rules.md BR1.1–BR6.5, functional-spec.md WF1–WF6), `nfr-requirements/` and `nfr-design/` (security layers, fixed DB path, logging, components). Contracts C1/C3 in `inception/contract-design/contract-summary.md`. Unit scope in `inception/units-generation/unit-of-work.md`. Stories US1.1–US1.5, US4.1.

Review findings carried into this plan: numeric enums rejected (JSON number and numeric string), missing required enum fields give 400, one validation-error path (`ValidationFailedException` + the framework's model-state 400 share one ProblemDetails shape with camelCase field keys and "allowed values" text), timestamps stored as UTC `DateTime` and read back with `DateTimeKind.Utc` (SQLite cannot order `DateTimeOffset`), enums stored as text (`HasConversion<string>()`), exceptions logged once (built-in exception-handler middleware logging silenced in favour of our handler), DB path rewrite unit-tested.

## How the work is approved

Per your request, the steps are built in **5 tasks**; after each task I stop, show what was done, tick it in `docs/progress.md`, and wait for your approval before the next task.

## Steps

### Task A — Project skeleton and test runner

- [ ] **Step 1 — Repository setup** (team practices). Rename branch `master` to `main`; commit the existing AI-DLC setup, `docs/` and the approved plan as `chore: add AI-DLC setup and approved plan`; create branch `unit/u1-foundation-tickets`. Add standard .NET entries to `.gitignore` (`bin/`, `obj/`, `*.user`, `.vs/`, `TestResults/`, `*.db`, `*.db-shm`, `*.db-wal`, `.env`) and a root `.editorconfig` (standard C# conventions, file-scoped namespaces, `_camelCase` private fields).
- [x] **Step 2 — Project structure and production configuration skeleton.** `dotnet new sln -n SupportDesk`; `dotnet new webapi --use-controllers -n SupportDesk.Api -o src/SupportDesk.Api` (net8.0, Nullable and ImplicitUsings on, `GenerateDocumentationFile` on); remove the template WeatherForecast files; add packages `Microsoft.EntityFrameworkCore.Sqlite` 8.x, `Microsoft.EntityFrameworkCore.Design` 8.x; keep `Swashbuckle.AspNetCore`; install local tool `dotnet-ef` 8.x via a tool manifest (`.config/dotnet-tools.json`). Folders: `Domain/`, `Tickets/`, `Tickets/Dtos/`, `Infrastructure/`, `Errors/`. (US1.1)
- [x] **Step 3 — Bootstrap the test runner.** `dotnet new xunit -n SupportDesk.Api.Tests -o tests/SupportDesk.Api.Tests`; reference the API project; add `Microsoft.AspNetCore.Mvc.Testing` 8.x, `Microsoft.EntityFrameworkCore.Sqlite` 8.x, `coverlet.collector`; add both projects to the solution; add `public partial class Program {}` for WebApplicationFactory; one smoke test tagged `[Trait("Unit","U1")]`. Verify the unit-scoped command `dotnet test tests/SupportDesk.Api.Tests --filter "Unit=U1"` runs green.

### Task B — Data model and database

- [ ] **Step 4 — Data model / database behavior: implement.** `Domain/Ticket.cs` (13 fields per entities.md), `Domain/TicketStatus.cs`, `TicketPriority.cs`, `TicketCategory.cs`, `TicketSentiment.cs` (shared enums, contract C3). `Infrastructure/SupportDeskDbContext.cs`: column max lengths (title 200, description 4000, aiSummary 500, suggestedAction 500, draftReply 4000), enums `HasConversion<string>()`, index on `CreatedAt`, UTC value converter for `DateTime` (`DateTimeKind.Utc` on read). `Infrastructure/IClock.cs` + `SystemClock.cs` (UTC now). `Infrastructure/DatabasePath.cs`: resolves `Data Source=App_Data/supportdesk.db` to an absolute path under the content root and creates `App_Data/`. Register in `Program.cs`; connection string in `appsettings.json`. Create migration `InitialCreate` with `dotnet ef migrations add InitialCreate --project src/SupportDesk.Api --output-dir Infrastructure/Migrations`. No auto-migrate at startup. (US1.1 AC1.1.4; BR1.5; NFR10.1–NFR10.2)
- [ ] **Step 5 — Data model / database behavior: write and run its tests.** `tests/.../Infrastructure/TicketPersistenceTests.cs`: save and reload keeps every field; enums stored as text; timestamps come back as UTC; ordering by `CreatedAt` works in SQLite; `DatabasePathTests`: relative path becomes absolute under the content root and the folder is created. Run the unit command.

### Task C — Errors and ticket business logic

- [ ] **Step 6 — Business logic: implement.** `Errors/`: `TicketNotFoundException`, `ValidationFailedException` (field -> messages), and the AI failures `AiInvalidOutputException`, `AiProviderException`, `AiNotConfiguredException`, `AiTimeoutException` (defined here, thrown by U2 — contract C3); `GlobalExceptionHandler : IExceptionHandler` mapping 404/400/502/502/503/504/500, logging unexpected errors once with `TraceId`; ProblemDetails customisation adding `traceId`. `Tickets/ITicketService.cs` + `TicketService.cs`: Create (trim, BR1.1–BR1.4), List (filters + ordering, AsNoTracking, BR2.1–BR2.4), Get (BR3.1, BR3.3), Update (BR4.1–BR4.3), ChangeStatus (BR5.1, BR5.2, BR4.3); every method async with `CancellationToken`; no AI dependency (BR6.5). (US1.1–US1.5, US4.1)
- [ ] **Step 7 — Business logic: write and run its tests.** `tests/.../Tickets/TicketServiceTests.cs` against SQLite in-memory with a fake clock: defaults on create; trimming; title/description boundaries (2/3/200/201 and 9/10/4000/4001 after trim, whitespace-only); list order and tie-break; each filter and combined filters; get/update/status not-found; update keeps status and AI fields; updatedAt refreshed on update and status; any-to-any status. `tests/.../Errors/GlobalExceptionHandlerTests.cs`: each exception type maps to its status code; 500 body has no stack trace or exception type and has `traceId`. Run the unit command.

### Task D — API endpoints

- [ ] **Step 8 — API / endpoint: implement.** `Tickets/Dtos/CreateTicketRequest`, `UpdateTicketRequest`, `UpdateStatusRequest` (nullable required fields + `[Required]`), `TicketResponse`. `Tickets/TicketsController.cs` (thin; `{id}` validated in the action — non-numeric or < 1 gives 400, BR3.2; filters bound as strings and parsed case-insensitively, rejecting numeric strings; empty filter = absent). `Program.cs`: controllers with `JsonStringEnumConverter(allowIntegerValues: false)` plus a check that rejects numeric strings; `InvalidModelStateResponseFactory` producing the same ProblemDetails shape as `ValidationFailedException`; `AddProblemDetails`, `AddExceptionHandler<GlobalExceptionHandler>`, `UseExceptionHandler()` in every environment; Swagger in Development with XML comments and enum names; logging levels per observability design. (WF1–WF6; contract C1)
- [ ] **Step 9 — API / endpoint: write and run its tests.** `tests/.../Api/TicketsApiTests.cs` with a `WebApplicationFactory` that swaps the DbContext to an open SQLite in-memory connection: create 201 + Location; blank title 400 naming `title`; list empty `[]`; list filters incl. lower-case and category; invalid filter 400 listing allowed values; get 404 naming id; `/api/tickets/abc` and `/0` give 400; PUT missing priority 400; PUT with `status: Closed` leaves status unchanged; PATCH `{ "status": "Done" }`, `{ "status": 1 }`, `{ "status": "1" }` and `{}` give 400; malformed JSON 400; a throwing service gives 500 without stack trace and with `traceId`. `tests/.../Api/RestartPersistenceTests.cs`: create a ticket via a host on a temporary SQLite file, dispose, start a new host on the same file, GET returns it (AC1.1.4). Run the unit command.

### Task E — Configuration, docs and checks

- [ ] **Step 10 — Environment/build configuration.** `appsettings.json` (connection string, logging levels; no secrets), `appsettings.Development.json`; `coverlet` settings excluding `Infrastructure/Migrations/` and `Program.cs` (team coverage rule); `.http` stub not needed yet (U4). Run `dotnet ef database update --project src/SupportDesk.Api` once to prove the DB is created at `src/SupportDesk.Api/App_Data/supportdesk.db`; run the API and check Swagger lists the 5 endpoints.
- [ ] **Step 11 — Documentation and traceability.** XML doc comments on controller actions and DTOs; `dotnet format`; run `dotnet build && dotnet test` and coverage (`dotnet test --collect:"XPlat Code Coverage"`), report the line coverage of hand-written code (target ≥ 80%; any shortfall is reported, never hidden by changing thresholds); small Conventional Commits per task on the unit branch.

## Story-to-Step Traceability

| Story | Steps |
|---|---|
| US1.1 Create | 2, 4, 5, 6, 7, 8, 9 |
| US1.2 List & filter | 6, 7, 8, 9 |
| US1.3 View | 6, 7, 8, 9 |
| US1.4 Edit | 6, 7, 8, 9 |
| US1.5 Status | 6, 7, 8, 9 |
| US4.1 Safe errors | 6, 7, 8, 9 |

## Testing Contract

```json
{
  "version": 1,
  "methodology": "test-after",
  "source": "team",
  "ordering": "For each unit, we implement each testable layer (DTO validation, then services, then controllers) and then write and run that layer's tests before we move to the next layer or merge the unit into `main`.",
  "scope": "classic",
  "test_strategy": "standard",
  "project_type": "greenfield",
  "applicable_notes": [
    {
      "layer": "org",
      "text": "We treat tests as a first-class deliverable in every Bolt. The specific\nmethodology (TDD, BDD, ATDD, or classic test-after) is affirmed at\npractices-discovery and recorded in `team.md` under this heading with explicit\n`Methodology` and `Ordering` fields; Code Generation resolves those fields\nindependently from coverage, tooling, and scope notes.\n\nWhen no posture has been affirmed, our default per scope is:\n- **Methodology**: test-after\n- **Ordering**: implement each applicable testable layer, then write and run\n  that layer's tests.\n- `mvp`, `enterprise`, `feature`, `infra`, `classic` add an 80% line-coverage\n  floor and CI execution before merge.\n- `bugfix`, `security-patch` add a targeted regression for the specific\n  bug/vulnerability and require the existing suite to remain green.\n- `express` uses the Minimal strategy: requirement-driven unit tests (one per\n  requirement, with a happy-path floor per component); existing tests remain\n  green.\n- `poc`, `refactor`, `workshop` add no extra new-test floor and require the\n  existing suite to remain green.\n\nThe active `Test Strategy` still applies in every scope and determines test\nvolume/types. Scope floors are additive; they never reduce or replace the\nselected strategy.\n\nBuild and Test verifies defined coverage floors and affirmed quality targets;\nthey may not be weakened to make a step pass.\n\nAffirm a stricter posture in `team.md` if the team commits to one."
    },
    {
      "layer": "team",
      "text": "- **Methodology**: test-after\n- **Methodology evidence**: The trainee chose it in the interview (Q4, option A).\n  It suits a solo developer learning C# and ASP.NET Core under a 5-day deadline,\n  and the brief asks for tests of the key business rules and AI-output\n  validation without requiring tests to be written first.\n- **Ordering**: For each unit, we implement each testable layer (DTO validation,\n  then services, then controllers) and then write and run that layer's tests\n  before we move to the next layer or merge the unit into `main`.\n- **Framework**: xUnit with the built-in `Assert`, in a separate test project in\n  the solution. We do not use extra assertion or mocking libraries.\n- **Fakes**: hand-written fakes for `IAiTicketService` and other interfaces.\n  Automated tests never call a real AI provider. Canned fake responses cover:\n  - valid output;\n  - malformed JSON;\n  - out-of-range enum values;\n  - a timeout;\n  - provider errors.\n- **Database tests**: service tests use EF Core against SQLite in-memory mode\n  (`DataSource=:memory:` with a connection held open), not the EF Core InMemory\n  provider.\n- **API tests**: a few `WebApplicationFactory` tests cover the main endpoints.\n  Each test replaces `IAiTicketService` with a fake and uses its own SQLite\n  in-memory database.\n- **Behaviour we always cover**:\n  - ticket validation (required fields, length limits);\n  - status values (only Open, InProgress, Resolved and Closed are accepted);\n  - 404 for unknown IDs;\n  - AI-output validation: typed DTO and allowed values, and malformed output is\n    not saved;\n  - AI timeouts and provider failures return a controlled error;\n  - draft-only replies;\n  - dashboard counts.\n- **Coverage floor**: at least 80% line coverage, measured with coverlet\n  (`dotnet test --collect:\"XPlat Code Coverage\"`) on hand-written code only.\n  EF Core migrations and `Program.cs` are excluded from measurement. We do not\n  lower this floor to make a step pass.\n- **Checks before merge (in place of CI)**: there is no CI service. Before a unit\n  branch is merged into `main` and before every push, `dotnet build` and\n  `dotnet test` must pass locally, and `dotnet format --verify-no-changes` must\n  report no changes. We never merge or push a failing build.\n- **Test strategy**: Standard (from the scope). The notes above add to it and\n  never replace it."
    },
    {
      "layer": "project",
      "text": "- Keep the 80% line-coverage floor, measured on hand-written code only (EF migrations and Program.cs excluded); with no CI in scope, a local dotnet build + dotnet test + dotnet format --verify-no-changes run stands in for CI before every merge and push (escalated: knowingly departs from the org CI-before-merge default). (learned 2026-10-19)"
    }
  ],
  "obligations": {
    "strategy": "standard",
    "strategy_volume": [
      "Five to eight tests per component.",
      "Unit tests plus integration tests for key boundaries.",
      "Add E2E, performance, or security tests when requirements demand them."
    ],
    "scope_floor": [
      "Keep the existing test suite green.",
      "This scope adds no extra new-test floor beyond the selected test strategy."
    ],
    "combination_rule": "Apply every selected-strategy obligation and every scope-floor obligation; neither replaces the other, and a targeted scope regression may add the narrowest necessary test type beyond the strategy default."
  },
  "plan_profile": {
    "methodology": "test-after",
    "runner_step": "Bootstrap the minimal test runner/configuration and record the exact unit-scoped command.",
    "runner_ready_before_first_test": true,
    "testable_layers": [
      "Data model / database behavior",
      "Repository / data access",
      "Business logic",
      "API / endpoint",
      "Frontend behavior"
    ],
    "steps": [
      "Project structure and production configuration skeleton.",
      "Bootstrap the minimal test runner/configuration and record the exact unit-scoped command.",
      "Data model / database behavior - implement.",
      "Data model / database behavior - write and run its tests after implementation.",
      "Repository / data access - implement.",
      "Repository / data access - write and run its tests after implementation.",
      "Business logic - implement.",
      "Business logic - write and run its tests after implementation.",
      "API / endpoint - implement.",
      "API / endpoint - write and run its tests after implementation.",
      "Frontend behavior - implement.",
      "Frontend behavior - write and run its tests after implementation.",
      "Environment/build configuration.",
      "Documentation and traceability."
    ]
  },
  "input_sha256": "sha256:95d2aef0dfdaf46cfe7ccac2228e04bdeabb99913f63bbb467ab73485bd46aab",
  "contract_sha256": "sha256:69e76c0c6539fd16cf5295d62551b49883346a948a0f4c59df88fab6fae6301e"
}
```

Layer notes: "Repository / data access" is folded into the data layer (Steps 4–5) because services use the DbContext directly (ADR-004); "Frontend behavior" does not apply (API only).
