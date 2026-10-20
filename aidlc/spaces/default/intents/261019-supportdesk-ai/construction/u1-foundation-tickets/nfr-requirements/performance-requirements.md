# Performance Requirements — U1 Foundation & Tickets

Sources: NFR6 and NFR7 in `inception/requirements-analysis/requirements.md`; workflows WF1–WF5 in `construction/u1-foundation-tickets/functional-design/functional-spec.md` and rules in `rules.md`; endpoints in contract C1 of `inception/contract-design/contract-summary.md`.

## Targets

| ID | Requirement | Target | How it is checked |
|---|---|---|---|
| NFR6.1 | Ticket endpoints (create, list, get, update, status) respond quickly on a developer machine | < 500 ms per request with up to 1,000 tickets in the database | Manual timing in Swagger with seeded data; not a CI gate |
| NFR6.2 | Filtering and ordering run in the database, not in memory | List queries translate to one SQL query with WHERE/ORDER BY (BR2.1, BR2.2) | Code review of the query |
| NFR6.3 | Reads that do not change data are not tracked by EF | No-tracking queries for list and get | Code review |
| NFR7.1 | All database calls are asynchronous and cancellable | Every EF call is `async` and receives the request's `CancellationToken` | Code review; analyzer warnings |

## Not Targeted

No throughput or concurrency benchmarks: single local user (assumption A1). No caching (data is small and must be current).
