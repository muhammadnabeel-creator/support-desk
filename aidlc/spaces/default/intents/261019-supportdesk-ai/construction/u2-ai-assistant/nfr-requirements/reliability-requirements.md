# Reliability Requirements — U2 AI Assistant

Sources: NFR1 and NFR3 in `inception/requirements-analysis/requirements.md`; hard rule ALWAYS keep core operations working in `aidlc/spaces/default/memory/project.md`; rules BR7.6, BR10.1–BR10.4 in `construction/u2-ai-assistant/functional-design/rules.md`; failure map in `functional-spec.md`; contract C4 in `inception/contract-design/contract-summary.md`.

| ID | Requirement | Target / check |
|---|---|---|
| NFR1.2 | AI problems never affect ticket CRUD or the dashboard | U1 endpoints have no dependency on `IAiTicketService`; app starts without a key |
| NFR1.3 | A missing key is detected per request, not at startup | Analyze/draft return 503 with no network call (BR10.1) |
| NFR3.8 | Failures are classified exactly once | Timeout → `AiTimeoutException` (504); client disconnect → plain cancellation (no 504, no write); HTTP non-2xx, network error, missing `choices[0].message.content` → `AiProviderException` (502); bad content → `AiInvalidOutputException` (502) |
| NFR3.9 | No partial writes | The ticket is saved only after a validated result; any failure leaves it unchanged (BR7.6) |
| NFR3.10 | Invalid configuration fails clearly | Timeout outside 1–120, empty/non-HTTPS base URL, or empty model → startup error naming the `Ai:*` key; empty/whitespace key → treated as "not configured" (503 per request) |
| NFR3.11 | No retries | BR10.4; the user can call again |

Contract C4's `timeout_or_cancel` mapping is corrected here: only the AI timeout maps to 504; a client disconnect is a cancellation (U1 handler: 499, no body, nothing saved).
