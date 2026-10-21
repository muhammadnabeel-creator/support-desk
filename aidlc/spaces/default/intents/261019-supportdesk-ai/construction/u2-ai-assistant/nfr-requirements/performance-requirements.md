# Performance Requirements — U2 AI Assistant

Sources: NFR3 in `inception/requirements-analysis/requirements.md`; contract C4 in `inception/contract-design/contract-summary.md`; workflows WF7–WF8 in `construction/u2-ai-assistant/functional-design/functional-spec.md` and rules BR10.2, BR10.4 in `rules.md`.

| ID | Requirement | Target | Check |
|---|---|---|---|
| NFR3.1 | Every Groq call is bounded by a timeout | Default 20 s (`Ai:TimeoutSeconds`, allowed 1–120); a timeout returns 504 within timeout + 2 s | Manual test with a tiny timeout; fake-based test in Task F |
| NFR3.2 | Client cancellation stops the Groq call immediately | The request `CancellationToken` is linked to the HTTP call | Code review |
| NFR3.3 | HTTP connections are reused | One typed `HttpClient` from `IHttpClientFactory`, not `new HttpClient()` per call | Code review |
| NFR3.4 | Responses are kept small | Analysis requested as JSON with `max_tokens` ≈ 600; drafts with `max_tokens` ≈ 900 and an instruction to stay under ~2,500 characters, so valid drafts fit the 4,000-character limit | Manual test |
| NFR3.5 | No retries add latency | One attempt per request (BR10.4) | Code review |

Non-AI endpoints are unaffected (U1 targets still apply; NFR1).
