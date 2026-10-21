# Tech Stack Decisions — U2 AI Assistant

Sources: FR11 (Groq only) in `inception/requirements-analysis/requirements.md`; contracts C2, C4 in `inception/contract-design/contract-summary.md`; U1 stack (.NET 8, System.Text.Json, built-in DI and logging); team Code Style; `construction/u2-ai-assistant/functional-design/functional-spec.md` and `rules.md`.

| ID | Area | Choice | Why |
|---|---|---|---|
| NFR8.11 | HTTP client | Built-in `IHttpClientFactory` typed client for `GroqAiTicketService` (`Microsoft.Extensions.Http`, part of ASP.NET Core) | Connection pooling; no extra package |
| NFR8.12 | Groq API | Plain HTTPS + System.Text.Json against `POST {BaseUrl}/chat/completions` (OpenAI-compatible); request `model`, `messages`, `temperature` 0.2, `max_tokens`, and `response_format: {type: json_object}` for analysis | No SDK dependency; easy to explain and to fake in tests |
| NFR8.13 | Configuration | `AiOptions` bound from section `Ai` with keys `Ai:Groq:ApiKey`, `Ai:Groq:Model` (default `openai/gpt-oss-120b`), `Ai:Groq:BaseUrl` (default `https://api.groq.com/openai/v1`), `Ai:TimeoutSeconds` (default 20); options validated at startup (`ValidateOnStart`) except the key | Matches contract C4; clear errors |
| NFR8.14 | Secrets | `dotnet user-secrets` (UserSecretsId added to the API csproj) for Development; environment variables otherwise | Team Deployment practice; hard rule |
| NFR8.15 | Code location | `src/SupportDesk.Api/Ai/`: `IAiTicketService`, `GroqAiTicketService`, `AiOutputValidator`, `AiPrompts`, `AiOptions`, `AiAnalysisResult`, `AiDraftResult`, Groq wire DTOs; analyze/draft added to `Tickets/` (service methods, controller actions, DTOs) | Components: AiAssistant + Tickets flows (ADR-001/002) |
| NFR9.3 | Testability | `GroqAiTicketService` takes `HttpClient` so Task F can use a fake `HttpMessageHandler`; Tickets flow tests use a hand-written `IAiTicketService` fake | Team Testing Posture (no real AI in tests) |

No new NuGet packages. No mock provider (decision 2026-10-20).
