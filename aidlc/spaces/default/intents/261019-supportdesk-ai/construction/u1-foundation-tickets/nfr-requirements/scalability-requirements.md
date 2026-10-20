# Scalability Requirements — U1 Foundation & Tickets

Sources: assumption A1 and NFR6 in `inception/requirements-analysis/requirements.md`; out-of-scope list there; FR3.3 (no pagination); single-deployable model in `inception/units-generation/unit-of-work.md`; functional rules in `construction/u1-foundation-tickets/functional-design/rules.md` and `functional-spec.md`.

## Requirements

| ID | Requirement | Target |
|---|---|---|
| NFR6.4 | Data volume the design must handle without change | Up to 1,000 tickets (local demo scale); list returns all matching tickets (no pagination, FR3.3) |
| NFR6.5 | Concurrency | One local user; requests are stateless; last write wins on concurrent edits (assumption A5) |

## Scaling Strategy

None required. Production scaling is out of scope. If the list ever grows large, pagination can be added later as an additive, optional query parameter (contract C1 additive-change rule).
