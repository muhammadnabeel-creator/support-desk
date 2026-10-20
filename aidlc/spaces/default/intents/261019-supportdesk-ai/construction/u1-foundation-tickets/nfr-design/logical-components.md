# Logical Components — U1 Foundation & Tickets

Sources: `construction/u1-foundation-tickets/nfr-requirements/tech-stack-decisions.md` (NFR8.9 layout) and the other U1 NFR requirement files; components in `inception/domain-design/components.md`; contract C3 in `inception/contract-design/contract-summary.md`; workflows in `construction/u1-foundation-tickets/functional-design/functional-spec.md`.

## Inventory

| Logical component | Location | Responsibility | NFR patterns applied | Failure domain |
|---|---|---|---|---|
| TicketsController | `src/SupportDesk.Api/Tickets/` | Maps HTTP to service calls; id validation | Input validation (layer 1), no over-posting | Per request |
| TicketService | `Tickets/` | Rules BR1–BR5; only writer of Ticket | Async + cancellation, no-tracking reads, single SaveChanges | Per request |
| Ticket DTOs | `Tickets/Dtos/` | Request/response shapes (nullable required fields) | Missing-field detection, over-posting guard | — |
| Domain types | `Domain/` | `Ticket` entity, 4 enums (shared with U2/U3) | Enum-by-name | — |
| SupportDeskDbContext + Migrations | `Infrastructure/` | Persistence, schema, column limits, `CreatedAt` index | Fixed DB path, migrations-only | SQLite file |
| Error handling | `Errors/` | Typed exceptions (C3), `IExceptionHandler`, ProblemDetails + `traceId` | No leakage, single log line | Process-wide (catches all) |
| Clock | `Infrastructure/` | UTC now | Testability | — |
| Program.cs composition | `src/SupportDesk.Api/` | DI, JSON options, Swagger, DB path, middleware order | Exception handler in all environments | Startup |

## Blast Radius

- A database failure affects ticket endpoints (generic 500) but never leaks details.
- The AI service (U2) is not referenced by any U1 component, so AI failures cannot affect U1 endpoints (NFR1.1).
- Shared plumbing (error handler, domain enums) is the only part U2/U3 depend on; changes there need checking against all units.
