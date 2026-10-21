# Performance Design — U2 AI Assistant

Sources: `construction/u2-ai-assistant/nfr-requirements/performance-requirements.md` (NFR3.1–NFR3.5), `tech-stack-decisions.md`; workflows WF7–WF8 in `construction/u2-ai-assistant/functional-design/functional-spec.md`; contract C4 in `inception/contract-design/contract-summary.md`.

| Requirement | Design |
|---|---|
| NFR3.1 timeout | Typed `HttpClient` with `HttpClient.Timeout = Infinite`; each call uses `CancellationTokenSource.CreateLinkedTokenSource(requestToken)` + `CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds))`. When the linked token fires and the request token did not, it is a timeout. |
| NFR3.2 cancellation | The request `CancellationToken` flows controller → TicketService → `IAiTicketService` → `SendAsync`/`ReadAsStreamAsync`. |
| NFR3.3 pooling | `builder.Services.AddHttpClient<IAiTicketService, GroqAiTicketService>(c => c.BaseAddress = …)`. |
| NFR3.4 small responses | Analysis: `max_tokens` 400, `temperature` 0.2, `response_format: json_object`. Draft: `max_tokens` 700 (≈2,800 characters) and an instruction "under 2,000 characters, plain text, no markdown". A response with `finish_reason = "length"` is treated as invalid output (truncated). |
| NFR3.5 no retries | No Polly/resilience handler is added; one `SendAsync` per request. |

The DB write happens once, after the AI result is validated, so the request holds no DB transaction while waiting for Groq.
