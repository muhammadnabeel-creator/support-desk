# Observability Design — U2 AI Assistant

Sources: `construction/u2-ai-assistant/nfr-requirements/observability-requirements.md` (NFR2.18–NFR2.20), `security-requirements.md` (NFR2.15–NFR2.16); failure map in `construction/u2-ai-assistant/functional-design/functional-spec.md`.

| Requirement | Design |
|---|---|
| NFR2.18 | `GroqAiTicketService` uses a `Stopwatch` and logs once in a `finally`: `AI {Operation} via {Model} -> {Outcome} in {ElapsedMs} ms (HTTP {StatusCode})`, where Outcome ∈ ok, not-configured, timeout, provider-error, invalid-output, cancelled. Level Information for ok, Warning otherwise. |
| NFR2.19 | U1 `GlobalExceptionHandler` changes AI-failure logging to type name + TraceId without the exception object (see security design). The adapter's own line carries the outcome, so the failure is visible once per layer without text content. |
| NFR2.20 | `appsettings.json` Logging adds `"System.Net.Http.HttpClient": "Warning"`. |
