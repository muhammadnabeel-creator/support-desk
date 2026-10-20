# Security Requirements — U1 Foundation & Tickets

Sources: NFR2 and assumption A1 in `inception/requirements-analysis/requirements.md`; hard rules in `aidlc/spaces/default/memory/project.md`; error rules BR6.1–BR6.3 and id rule BR3.2 in `construction/u1-foundation-tickets/functional-design/rules.md`; workflows in `functional-spec.md`; contract C1 conventions in `inception/contract-design/contract-summary.md`.

## Requirements

| ID | Requirement | Rationale |
|---|---|---|
| NFR2.1 | Error responses never include stack traces, exception type names, inner exception messages, connection strings or configuration values; unexpected errors return a generic 500 ProblemDetails (BR6.2) | Hard rule: no leakage |
| NFR2.2 | The developer exception page is not used for API responses in any environment; the global handler always produces ProblemDetails | Stops leakage in Development too, where the reviewer runs it |
| NFR2.3 | Logs never contain request bodies, ticket text, secrets or connection strings; errors are logged with type and message server-side only | Hard rules; personal-data caution |
| NFR2.4 | All input is validated at the boundary: lengths (BR1.1, BR1.2), enum names (BR2.3, BR4.1, BR5.2), id format (BR3.2), body readability (BR6.3); invalid input is rejected with 400 before any database work | Construction guardrail: validate all inputs |
| NFR2.5 | Data access uses EF Core LINQ only; no raw SQL is built from input | SQL injection prevention (ADR-004) |
| NFR2.6 | No secrets exist in U1 code or committed configuration; the SQLite connection string is a relative file path with no credentials; `.gitignore` excludes `*.db`, `*.db-shm`, `*.db-wal`, `.env`, `bin/`, `obj/` | Hard rule: never commit secrets |
| NFR2.7 | Over-posting is impossible: request DTOs contain only the fields a client may set (create: title, description; update: title, description, priority; status: status), never the entity | Protects status/AI fields (BR4.2) |
| NFR2.8 | Text is stored and returned as data; JSON serialization escapes it; nothing is executed or rendered as HTML by the API | Untrusted input handling |

## Threat Notes

- **Authentication/authorization**: none, by design (brief marks auth out of scope; A1 single local user). The README must state the API is for local use only. Flagged per the construction guardrail on code without auth.
- **Denial of service**: request body size uses the framework default limit; text lengths are capped by validation. No rate limiting (local use).
- **HTTPS**: the template's HTTPS redirection stays on; local development certificate is acceptable.
