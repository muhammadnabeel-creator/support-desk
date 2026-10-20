# Performance Design — U1 Foundation & Tickets

Sources: `construction/u1-foundation-tickets/nfr-requirements/performance-requirements.md` (NFR6.1–NFR6.3, NFR7.1), `scalability-requirements.md`, `tech-stack-decisions.md`; workflows in `construction/u1-foundation-tickets/functional-design/functional-spec.md`; contract C1 in `inception/contract-design/contract-summary.md`.

## Design

| Requirement | Design solution |
|---|---|
| NFR6.1 (< 500 ms at 1,000 tickets) | Single SQLite file, one EF query per request, no N+1 patterns (Ticket has no relationships). An index on `CreatedAt` supports the default ordering. |
| NFR6.2 (filter/sort in database) | The list service builds one `IQueryable<Ticket>`, adds a `Where` per present filter, then `OrderByDescending(CreatedAt).ThenByDescending(Id)`, and materialises once with `ToListAsync`. Filters are compared as enum values (stored as text), so SQLite does the work. |
| NFR6.3 (no tracking for reads) | List and get use `AsNoTracking()`; update and status load tracked entities. |
| NFR7.1 (async + cancellation) | Every controller action takes a `CancellationToken` and passes it to service methods, which pass it to `FirstOrDefaultAsync`, `ToListAsync`, `SaveChangesAsync`. No `.Result` or `.Wait()`. |

## Explicitly Not Used

No caching (data must be current; small dataset), no response compression, no pagination (FR3.3).
