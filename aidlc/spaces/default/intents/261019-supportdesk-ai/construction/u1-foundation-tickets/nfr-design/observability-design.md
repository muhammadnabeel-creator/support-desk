# Observability Design — U1 Foundation & Tickets

Sources: `construction/u1-foundation-tickets/nfr-requirements/observability-requirements.md` (NFR2.9–NFR2.12), `security-requirements.md`, `tech-stack-decisions.md`; workflow WF6 in `construction/u1-foundation-tickets/functional-design/functional-spec.md`.

## Design

| Requirement | Design solution |
|---|---|
| NFR2.9 (console ILogger) | Default host logging; `appsettings.json` sets `Default: Information`, `Microsoft.AspNetCore: Warning`, `Microsoft.EntityFrameworkCore: Warning`. |
| NFR2.10 (log unexpected errors once) | The `IExceptionHandler` logs unhandled exceptions at Error with the exception object and the request's trace identifier as a structured property (`TraceId`), then writes the generic 500. |
| NFR2.11 (client errors not errors) | Validation 400s and 404s are not logged by the handler (typed not-found is logged at Information only). |
| NFR2.12 (traceId in responses) | `AddProblemDetails` customisation adds `traceId = HttpContext.TraceIdentifier` (or the current Activity id) to every ProblemDetails body; the same id is in the error log line from NFR2.10, so a 500 can be matched to its log entry. |

No metrics, tracing exporters, dashboards or alerts (out of scope).
