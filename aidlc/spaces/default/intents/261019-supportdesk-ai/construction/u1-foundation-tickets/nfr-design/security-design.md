# Security Design — U1 Foundation & Tickets

Sources: `construction/u1-foundation-tickets/nfr-requirements/security-requirements.md` (NFR2.1–NFR2.8), `observability-requirements.md`, `tech-stack-decisions.md` (NFR8.5); rules BR1.1–BR6.5 in `construction/u1-foundation-tickets/functional-design/rules.md` via `functional-spec.md`; contract C1/C3 in `inception/contract-design/contract-summary.md`; hard rules in `aidlc/spaces/default/memory/project.md`.

## Defense Layers (request order)

1. **Routing**: `{id}` routes are declared without an `:int` constraint; the id binds as a string-tolerant parameter and is validated by the action (BR3.2) so `abc` and `0` give 400, not 404.
2. **Body parsing**: System.Text.Json with `JsonStringEnumConverter(allowIntegerValues: false)` — numeric enum values (`"status": 1`) fail parsing -> 400 (BR6.3). Unknown JSON properties are ignored (contract additive rule) but can never reach the entity (layer 4).
3. **Validation**: request DTOs use nullable properties for every required field (`string?`, `TicketPriority?`, `TicketStatus?`) plus `[Required]`, so a **missing** enum field is a 400 instead of silently defaulting to `Low`/`Open`. Length checks run on trimmed text in the service (BR1.1, BR1.2) and are reported as field errors. Query filters are bound as strings and parsed with `Enum.TryParse(ignoreCase: true)` plus a "defined name, not a number" check (BR2.3); empty filter values (`?status=`) are treated as absent; a repeated filter uses the first value.
4. **Over-posting**: the entity is never bound from a request. Controllers map only the DTO fields each operation allows (NFR2.7, BR4.2).
5. **Data access**: EF Core LINQ only; no `FromSqlRaw`/string-built SQL (NFR2.5).
6. **Errors**: one `IExceptionHandler` plus `AddProblemDetails()`; `UseExceptionHandler()` is registered in every environment and `UseDeveloperExceptionPage` is not used (NFR2.1, NFR2.2). 500 bodies carry only a generic title/detail and the `traceId`.
7. **Logging**: EF `EnableSensitiveDataLogging` and `EnableDetailedErrors` stay off; request bodies are never logged (NFR2.3).

## Secrets and Files (NFR2.6)

- Connection string has no credentials: SQLite file at `App_Data/supportdesk.db` under the API project's content root (see `reliability-design.md`).
- `.gitignore` adds `bin/`, `obj/`, `*.user`, `.vs/`, `TestResults/`, `*.db`, `*.db-shm`, `*.db-wal`, `.env`.

## Verification

| Control | Verified by |
|---|---|
| Numeric enum rejected | API test: `{"status": 1}` -> 400 |
| Missing enum field rejected | API tests: PUT without `priority`, PATCH without `status` -> 400 |
| Bad id -> 400 | API test: `/api/tickets/abc` and `/api/tickets/0` -> 400 |
| No stack trace in 500 | API test with a service that throws: body has no "Exception"/stack text, has `traceId` |
| Over-posting blocked | API test: PUT with `status: Closed` leaves status unchanged |
| No secrets committed | `.gitignore` review; secrets check in U4 |

Auth is intentionally absent (local single user, out of scope); the README states the API is for local use only.
