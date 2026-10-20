# Tech Stack Decisions — U1 Foundation & Tickets

Sources: constraints in `inception/requirements-analysis/requirements.md`; ADR-004/005 behind `inception/domain-design/components.md`; contract C1 in `inception/contract-design/contract-summary.md`; team practices (Testing Posture, Code Style) in `aidlc/spaces/default/memory/team.md`; answers Q1–Q2 in `nfr-requirements-questions.md`; rules in `construction/u1-foundation-tickets/functional-design/rules.md` and `functional-spec.md`.

| ID | Area | Choice | Why |
|---|---|---|---|
| NFR8.1 | Runtime | .NET 8 (SDK 8.0.x, installed), C# 12, `net8.0` | Brief requires ASP.NET Core; LTS version installed |
| NFR8.2 | Web framework | ASP.NET Core Web API with controllers (`[ApiController]`) | Brief: controllers; built-in model validation and ProblemDetails |
| NFR8.3 | Data access | EF Core 8 with `Microsoft.EntityFrameworkCore.Sqlite`; `DbContext` used directly in services | Brief: EF Core + SQLite; ADR-004 |
| NFR8.4 | Migrations | `Microsoft.EntityFrameworkCore.Design` + the `dotnet-ef` tool (8.x) | Brief: EF migrations; team practice: documented `dotnet ef database update` |
| NFR8.5 | JSON | System.Text.Json with `JsonStringEnumConverter` (enum names, numbers rejected) | Contract C1: enums as names; review finding on numeric enums |
| NFR8.6 | Error responses | Built-in ProblemDetails + one `IExceptionHandler` | ADR-005; FR10 |
| NFR8.7 | API docs | Swashbuckle.AspNetCore (Swagger UI in Development), enums as names, XML comments | Q2; brief requires Swagger |
| NFR8.8 | Logging | Built-in `ILogger` console provider | Q1 |
| NFR8.9 | Project layout | One Web API project `src/SupportDesk.Api` + one test project `tests/SupportDesk.Api.Tests` in `SupportDesk.sln`; folders `Domain/` (entity, enums), `Tickets/` (controller, service, DTOs), `Infrastructure/` (DbContext, migrations), `Errors/` (exceptions, handler) | Units Generation: single deployable; shared Domain folder rule; no extra layers |
| NFR9.1 | Tests | xUnit + built-in Assert; `Microsoft.AspNetCore.Mvc.Testing` (WebApplicationFactory); SQLite in-memory | Team Testing Posture |
| NFR9.2 | Coverage | coverlet (`XPlat Code Coverage` collector), 80% line floor on hand-written code, excluding `Migrations/` and `Program.cs` | Team Testing Posture |
| NFR8.10 | Formatting | `.editorconfig` + `dotnet format`; nullable and implicit usings on | Team Code Style |

No other packages are added in U1.
