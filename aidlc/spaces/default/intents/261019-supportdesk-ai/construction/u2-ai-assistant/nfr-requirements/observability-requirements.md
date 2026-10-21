# Observability Requirements — U2 AI Assistant

Sources: NFR2 in `inception/requirements-analysis/requirements.md`; U1 logging design (console `ILogger`, `traceId` in ProblemDetails); rule BR10.5 in `construction/u2-ai-assistant/functional-design/rules.md` and failure map in `functional-spec.md`.

| ID | Requirement | Detail |
|---|---|---|
| NFR2.18 | One structured log line per AI call | Information: `AI {Operation} via {Model} -> {Outcome} in {ElapsedMs} ms (HTTP {StatusCode})`; no text content |
| NFR2.19 | AI failures logged once at Warning by U1's global handler | Exception type and generic message only (NFR2.15) with `TraceId`; the adapter does not log the same failure again |
| NFR2.20 | `HttpClient` default request logging does not leak the key | `System.Net.Http.HttpClient` log category set to Warning (default logging prints URLs, not headers; keep it quiet anyway) |

No metrics or tracing backends (out of scope).
