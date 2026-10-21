# Scalability Requirements — U2 AI Assistant

Sources: assumption A1 and NFR3 in `inception/requirements-analysis/requirements.md`; `construction/u2-ai-assistant/functional-design/functional-spec.md` and `rules.md` (BR10.4).

| ID | Requirement | Target |
|---|---|---|
| NFR3.6 | Concurrent AI requests | One local user; requests are independent; Groq rate limits surface as provider errors (502) with no retry |
| NFR3.7 | Shared resources | One pooled typed HttpClient; no in-memory state between requests |

No queues, caching of AI results, or batching (out of scope).
