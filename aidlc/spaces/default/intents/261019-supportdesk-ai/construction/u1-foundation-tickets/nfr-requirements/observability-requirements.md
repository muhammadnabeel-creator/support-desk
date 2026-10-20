# Observability Requirements — U1 Foundation & Tickets

Sources: NFR2 in `inception/requirements-analysis/requirements.md`; error rules BR6.1–BR6.2 in `construction/u1-foundation-tickets/functional-design/rules.md` and workflow WF6 in `functional-spec.md`; answer Q1 in `nfr-requirements-questions.md`.

## Requirements

| ID | Requirement | Detail |
|---|---|---|
| NFR2.9 | Logging uses the built-in ASP.NET Core `ILogger` writing to the console | Default level Information; `Microsoft.EntityFrameworkCore` at Warning to keep SQL out of normal logs |
| NFR2.10 | Every unexpected error is logged once, server-side, by the global handler | Logged at Error with exception type, message and stack trace in the console only; the client gets the generic 500 (NFR2.1) |
| NFR2.11 | Client errors (400/404) are not logged as errors | Logged at Information or not at all, to keep the console readable |
| NFR2.12 | ProblemDetails responses carry a `traceId` | So a reviewer can match a 500 response to its console log line |

## Not Required

No metrics, dashboards, alerting, tracing backends or SLOs: local-only project; Operation phase is out of scope.
