# Scalability Design — U2 AI Assistant

Sources: `construction/u2-ai-assistant/nfr-requirements/scalability-requirements.md` (NFR3.6, NFR3.7), `performance-requirements.md`, `tech-stack-decisions.md`; `construction/u2-ai-assistant/functional-design/functional-spec.md`.

| Requirement | Design |
|---|---|
| NFR3.6 | Stateless adapter registered as a typed client (transient per request); Groq HTTP 429 maps to `AiProviderException` (502) with no retry. |
| NFR3.7 | One pooled handler from `IHttpClientFactory`; no static state, no caches. |
