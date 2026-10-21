# Code Summary — U2 AI Assistant

## Files Created / Modified

**API — `src/SupportDesk.Api/`:**

| Folder | Files | Purpose |
|---|---|---|
| `Ai/` (new) | `IAiTicketService.cs`, `AiAnalysisResult.cs`, `AiDraftResult.cs`, `AiDraftContext.cs`, `AiOptions.cs`, `GroqOptions.cs`, `AiPrompts.cs`, `AiOutputValidator.cs`, `GroqAiTicketService.cs` | Contract C2: Groq adapter with timeout, failure classification, one log line per call; prompts with ticket data as escaped JSON; validation of AI output into typed results |
| `Ai/Groq/` (new) | `GroqChatRequest.cs`, `GroqMessage.cs`, `GroqResponseFormat.cs`, `GroqChatResponse.cs`, `GroqChoice.cs` | OpenAI-compatible wire shapes (snake_case) |
| `Tickets/` | `ITicketService.cs`, `TicketService.cs` (modified), `TicketAnalysisOutcome.cs` (new), `TicketsController.cs` (modified), `Dtos/AnalysisResponse.cs`, `Dtos/DraftReplyResponse.cs` (new) | `AnalyzeAsync` / `DraftReplyAsync` (load → AI → apply → one save) and `POST /api/tickets/{id}/analyze`, `POST /api/tickets/{id}/draft-reply` |
| `Errors/` | `GlobalExceptionHandler.cs` (modified) | AI failures logged as type + status + TraceId, without the exception object |
| root | `Program.cs`, `appsettings.json`, `SupportDesk.Api.csproj` (modified) | `AiOptions` bound and validated at startup (key not required), typed HttpClient, `Ai` section with empty key placeholder, HttpClient log level Warning, `UserSecretsId` |

No tests were added (deferred to Task F). No migration: U2 writes existing `Ticket` fields.

## Key Implementation Decisions

- **Groq only, no mock** (decision 2026-10-20). Without a key both AI endpoints return 503 and every ticket operation still works.
- **Timeout:** linked `CancellationTokenSource` with `CancelAfter(Ai:TimeoutSeconds)` (default 20); `HttpClient.Timeout` is infinite so only our timeout applies. Caller cancellation is rethrown (499), our timeout becomes `AiTimeoutException` (504); an `IOException` during a cancelled read is classified the same way.
- **Failure mapping:** network error, non-2xx (incl. 429), unreadable or empty envelope → `AiProviderException` (502); `finish_reason = "length"` or content failing validation → `AiInvalidOutputException` (502). Fixed generic messages; inner exceptions only for network and timeout errors; no retries.
- **Validation:** analysis must be one JSON object; only the five known fields read (exact names); enum values matched by name ignoring case, spaces, hyphens and underscores, numbers rejected; summary and suggested action 1–500 characters; draft 1–4000 characters.
- **Prompts:** instructions, schema and allowed values in the system message; title and description (and the saved analysis for drafts) serialized as a JSON object in the user message; id and timestamps never sent.
- **Secrets:** key only from user secrets or environment; Bearer header set per request; `ToString()` of the options hides the key; never logged.
- **Responses** read the saved ticket so they always match what was stored; draft notice is the constant "Draft — requires human review; not sent". No code path can send a reply.
- `GroqAiTicketService.CreateBaseAddress` keeps `/openai/v1` by ensuring a trailing slash (kept out of `Program.cs` so it can be tested in Task F).

## Test Coverage Summary

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: 18/18 existing tests pass. `dotnet format --verify-no-changes`: clean.
- Smoke check without a key: Swagger lists both endpoints; analyze and draft-reply on an existing ticket return 503 ProblemDetails with `traceId`; `abc` id → 400; unknown id → 404.
- **Not yet done (Task F):** the ~33 U2 tests in `unit-test-instructions.md` and the 80% coverage measurement. Groq with a real key is verified manually by the person.

## Deviations from the Plan

- Test steps 2, 4 and 6 deferred to Task F at the person's request; their boxes stay unticked.
- `GroqOptions` is its own file (one type per file) rather than a nested class; configuration keys are unchanged.
- `TicketAnalysisOutcome` added as a small result type (ticket + previous priority).
- After the code review (READY, `reviews/review-01.md`), the person chose to fix two Minor findings: R-01, the API key is trimmed when bound (`GroqOptions.ApiKey`); R-02, `max_tokens` raised to 600 (analysis) and 900 (draft) so non-Latin answers are not truncated. R-03 (comment wording), R-04 (OpenAPI vs C1, same as U1 R-02) and R-05 (README draft warning) are carried to U4.
- **Default model changed (2026-10-20):** the person's manual test returned 502; the adapter's log showed Groq HTTP 404 because `llama-3.3-70b-versatile` is no longer offered to the key. The person chose `openai/gpt-oss-120b` (tested: analyze 200 in ~1 s, draft-reply 200 in ~0.6 s, valid output). Changed in `GroqOptions.DefaultModel` and `appsettings.json`; requirements FR11.3, entities, tech-stack and security design updated. The model stays configurable through `Ai:Groq:Model`.
- No commits were made by the build: the person commits by hand.
