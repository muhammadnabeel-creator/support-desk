# Reliability Requirements — U1 Foundation & Tickets

Sources: NFR1 and NFR10 in `inception/requirements-analysis/requirements.md`; rule BR1.5 (durability) and BR6.5 (no AI calls) in `construction/u1-foundation-tickets/functional-design/rules.md`; workflows in `functional-spec.md`; team practices on migrations and rollback in `aidlc/spaces/default/memory/team.md`.

## Requirements

| ID | Requirement | Target / check |
|---|---|---|
| NFR1.1 | Ticket operations never depend on the AI service | Create, list, get, update and status work with no AI provider configured; U1 tests run with no AI service registered |
| NFR10.1 | Data survives application restarts | Writes are committed to the SQLite file before responding (BR1.5); verified by AC1.1.4 |
| NFR10.2 | Database schema is created only by migrations | `dotnet ef database update` creates the database; the app does not auto-migrate at startup; committed migrations are never edited |
| NFR10.3 | A missing database file gives a clear, documented fix | If the database has not been created, the README's first troubleshooting step is `dotnet ef database update`; the app returns a generic 500 rather than leaking details (NFR2.1) |
| NFR10.4 | Each write is all-or-nothing | One `SaveChanges` per request; a failed save leaves the previous data intact |
| NFR10.5 | Tests are repeatable and isolated | Each test uses its own SQLite in-memory database; no shared state between tests; no network |

## Recovery

Rollback is `git revert` or the previous tag; for data, delete the local SQLite file and run `dotnet ef database update` (team practices). No backups: local demo data only.
