# Scalability Design — U1 Foundation & Tickets

Sources: `construction/u1-foundation-tickets/nfr-requirements/scalability-requirements.md` (NFR6.4, NFR6.5), `performance-requirements.md`, `tech-stack-decisions.md`; `construction/u1-foundation-tickets/functional-design/functional-spec.md`; contract C1 additive-change rule in `inception/contract-design/contract-summary.md`.

## Design

| Requirement | Design solution |
|---|---|
| NFR6.4 (1,000 tickets, no pagination) | Full result list returned; text columns capped (title 200, description 4000) keep rows small. If growth ever requires it, add optional `page`/`pageSize` query parameters (additive change). |
| NFR6.5 (one user, last write wins) | Stateless controllers and scoped services/DbContext per request; no concurrency tokens; last `SaveChanges` wins. |

No horizontal scaling, load balancing or partitioning: one local process (out of scope).
