# Unit Test Instructions — U1 Foundation & Tickets

## Framework and Setup

- Test project: `tests/SupportDesk.Api.Tests` (xUnit, built-in `Assert`, no extra assertion or mocking libraries).
- Packages: `Microsoft.AspNetCore.Mvc.Testing` 8.x (WebApplicationFactory), `Microsoft.EntityFrameworkCore.Sqlite` 8.x, `coverlet.collector`.
- Every U1 test class carries `[Trait("Unit", "U1")]` so this unit's tests can be run alone.

## How to Run This Unit's Tests

Runnable once Step 3 has created the test project (before the first real test):

```bash
dotnet test tests/SupportDesk.Api.Tests --filter "Unit=U1"
```

With coverage:

```bash
dotnet test tests/SupportDesk.Api.Tests --filter "Unit=U1" --collect:"XPlat Code Coverage"
```

## Coverage Target

At least 80% line coverage of U1's hand-written code (`Domain/`, `Tickets/`, `Infrastructure/` except `Migrations/`, `Errors/`). `Program.cs` and `Infrastructure/Migrations/` are excluded. The threshold is never lowered to pass; a shortfall is reported.

## Test Volume (Standard strategy: 5–8 per component, plus key integration boundaries)

| Component | File | ~Tests |
|---|---|---|
| Persistence + DB path | `Infrastructure/TicketPersistenceTests.cs`, `Infrastructure/DatabasePathTests.cs` | 5 |
| TicketService | `Tickets/TicketServiceTests.cs` | 15 |
| Error handling | `Errors/GlobalExceptionHandlerTests.cs` | 5 |
| API (end to end) | `Api/TicketsApiTests.cs`, `Api/RestartPersistenceTests.cs` | 15 |

## Mocking and Stubbing

- Hand-written fakes only: `FakeClock` (fixed, advanceable UTC time). U1 never registers or calls an AI service.
- API tests: a custom `WebApplicationFactory<Program>` that removes the app's DbContext registration and adds one bound to an open `SqliteConnection("DataSource=:memory:")`; schema created with `EnsureCreated()` (test-only).
- The 500-path test registers a throwing `ITicketService` fake.

## Test Data

- Each test builds its own data through the service or DbContext; no shared fixtures with state.
- Text boundaries use generated strings (`new string('a', 200)`).
- The restart test uses a temporary SQLite file under the system temp folder, deleted afterwards.
