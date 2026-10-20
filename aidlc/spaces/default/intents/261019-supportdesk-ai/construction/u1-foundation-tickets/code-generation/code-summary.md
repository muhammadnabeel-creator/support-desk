# Code Summary — U1 Foundation & Tickets

## Files Created / Modified

**Repository:** `.gitignore` (+ .NET entries), `.editorconfig` (C# conventions; migrations marked generated), `.config/dotnet-tools.json` (dotnet-ef 8.0.32), `SupportDesk.sln`, `coverlet.runsettings` (excludes Migrations and Program.cs).

**API — `src/SupportDesk.Api/`:**

| Folder | Files | Purpose |
|---|---|---|
| `Domain/` | `Ticket.cs`, `TicketStatus.cs`, `TicketPriority.cs`, `TicketCategory.cs`, `TicketSentiment.cs` | Entity (13 fields) and the shared enums (contract C3) |
| `Infrastructure/` | `SupportDeskDbContext.cs`, `UtcDateTimeConverter.cs`, `DatabasePath.cs`, `IClock.cs`, `SystemClock.cs`, `StrictEnum.cs`, `StrictEnumConverter.cs`, `StrictEnumConverterFactory.cs`, `ApiParameterSchemaFilter.cs`, `Migrations/…InitialCreate*` | Persistence, UTC timestamps, fixed DB path, strict enum parsing, Swagger parameter docs |
| `Errors/` | `TicketNotFoundException.cs`, `ValidationFailedException.cs`, `AiInvalidOutputException.cs`, `AiProviderException.cs`, `AiNotConfiguredException.cs`, `AiTimeoutException.cs`, `GlobalExceptionHandler.cs`, `ProblemDetailsCustomizer.cs`, `InvalidModelStateResponse.cs` | One ProblemDetails error path with `traceId`; typed failures → 400/404/502/503/504; generic 500 |
| `Tickets/` | `ITicketService.cs`, `TicketService.cs`, `CreateTicketInput.cs`, `UpdateTicketInput.cs`, `TicketListFilter.cs`, `TicketsController.cs`, `Dtos/CreateTicketRequest.cs`, `Dtos/UpdateTicketRequest.cs`, `Dtos/UpdateStatusRequest.cs`, `Dtos/TicketResponse.cs` | Business rules BR1–BR5 and the 5 endpoints |
| root | `Program.cs`, `appsettings.json`, `appsettings.Development.json`, `SupportDesk.Api.csproj` | Composition, JSON options, Swagger, logging, connection string |

**Tests — `tests/SupportDesk.Api.Tests/`:** project scaffold and `SmokeTests.cs` (2 tests). Three early test files (`Infrastructure/DatabasePathTests.cs`, `Infrastructure/TicketPersistenceTests.cs`, `TestSupport/SqliteInMemoryDatabase.cs`, 16 tests) were written before tests were deferred and are kept uncommitted for Task F.

## Key Implementation Decisions

- **Feature-first order (person's request, 2026-10-20):** test steps 5, 7 and 9 are deferred to a later Task F; feature Steps 1–4, 6, 8, 10, 11 are done. The 80% coverage floor still applies at Task F.
- Timestamps stored as UTC `DateTime` with a converter (SQLite cannot order `DateTimeOffset`); responses always end in `Z`.
- Enums accepted only by name, case-insensitively (`StrictEnum`), in JSON bodies and query filters; numbers, numeric strings and combined flags rejected.
- `{id}` bound as a string and validated (non-numeric or < 1 → 400, not 404).
- `[Required]` on request DTOs plus service-level length checks; both produce the same ProblemDetails shape with camelCase keys and identical wording.
- Exception handler active in every environment; the framework's own exception log is filtered so each unexpected error is logged once with `TraceId`.
- Additions beyond the plan: client-cancelled requests → 499 (logged at Information); `BadHttpRequestException` keeps its status with a generic message; 415 for wrong content type.
- Database at `src/SupportDesk.Api/App_Data/supportdesk.db` via `DatabasePath` (content-root anchored); created only by `dotnet ef database update`.

## Test Coverage Summary

- Verification `dotnet build && dotnet test`: 0 warnings, 0 errors, 18/18 tests pass (2 smoke + 16 early persistence/path tests).
- Manual curl smoke run of all 5 endpoints and error cases matched contract C1, including a ticket surviving a server restart.
- **Not yet done (Task F):** service, error-handler and API end-to-end tests (Steps 7 and 9) and the 80% coverage measurement (`dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"`).

## Deviations from the Plan

- Test steps 5, 7, 9 deferred to Task F at the person's request; their boxes stay unticked.
- No commits were made by the build: the person commits by hand.
- Step 11's coverage measurement moved to Task F.
