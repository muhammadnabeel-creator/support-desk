# Reliability Design — U1 Foundation & Tickets

Sources: `construction/u1-foundation-tickets/nfr-requirements/reliability-requirements.md` (NFR1.1, NFR10.1–NFR10.5), `security-requirements.md`, `tech-stack-decisions.md`; `construction/u1-foundation-tickets/functional-design/functional-spec.md`; answer Q1 in `nfr-design-questions.md`.

## Database Location (review finding, Q1)

- The connection string in `appsettings.json` is `Data Source=App_Data/supportdesk.db` (relative).
- At startup, `Program.cs` rewrites the relative `Data Source` into an absolute path under `IHostEnvironment.ContentRootPath` (the API project folder) and creates the `App_Data` folder if missing.
- A design-time `IDesignTimeDbContextFactory` is not needed: `dotnet ef` runs the app's host with the API project as content root, so it resolves the same absolute path.
- Result: `dotnet ef database update` (from repo root with `--project src/SupportDesk.Api`, or from the project folder) and `dotnet run` always use `src/SupportDesk.Api/App_Data/supportdesk.db`.

## Design

| Requirement | Design solution |
|---|---|
| NFR1.1 (no AI dependency) | U1 services take only the DbContext and a clock; nothing in U1 references `IAiTicketService`. U1 API tests start the app without any AI service registered. |
| NFR10.1 (durable) | `SaveChangesAsync` completes before the response; SQLite file on disk. Verified by an API test that creates a ticket, disposes the host, starts a new host on the same file, and reads it back. |
| NFR10.2 (migrations only) | Initial migration `InitialCreate` committed under `Infrastructure/Migrations`; no `EnsureCreated`/`Migrate` at startup. Tests create their in-memory schema with `EnsureCreated` (test-only). |
| NFR10.3 (missing database) | If tables are missing, EF throws; the global handler returns a generic 500 and logs the cause; README troubleshooting says to run `dotnet ef database update`. |
| NFR10.4 (all-or-nothing) | One `SaveChangesAsync` per request (implicit transaction). |
| NFR10.5 (isolated tests) | Each test creates its own open `SqliteConnection("DataSource=:memory:")`; API tests replace the app's DbContext registration with it; no network calls. |

## Time

A small `IClock` (or `TimeProvider`) supplies UTC "now" so `createdAt`/`updatedAt` are testable; timestamps are stored as UTC.
