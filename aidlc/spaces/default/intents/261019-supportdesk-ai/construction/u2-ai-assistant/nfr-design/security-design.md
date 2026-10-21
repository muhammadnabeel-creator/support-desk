# Security Design — U2 AI Assistant

Sources: `construction/u2-ai-assistant/nfr-requirements/security-requirements.md` (NFR2.13–NFR2.17, NFR4.1–NFR4.3, NFR5.1–NFR5.2), `observability-requirements.md`, `reliability-requirements.md`, `tech-stack-decisions.md`; rules in `construction/u2-ai-assistant/functional-design/functional-spec.md`; contracts C2/C4 in `inception/contract-design/contract-summary.md`; hard rules in `aidlc/spaces/default/memory/project.md`.

## Secret Handling (NFR2.13, NFR2.14)

- `UserSecretsId` added to the API csproj; developers run `dotnet user-secrets set "Ai:Groq:ApiKey" "<key>" --project src/SupportDesk.Api`. Environment variable `Ai__Groq__ApiKey` also works.
- `appsettings.json` has `"Ai": { "Groq": { "ApiKey": "", "Model": "openai/gpt-oss-120b", "BaseUrl": "https://api.groq.com/openai/v1" }, "TimeoutSeconds": 20 }` — empty key placeholder only.
- The key is read from `IOptions<AiOptions>` per request and set as `Authorization: Bearer` on that request message only (not on `DefaultRequestHeaders`). `AiOptions` overrides `ToString()` to never print the key.

## Exceptions and Logging (NFR2.15, NFR2.16)

- The adapter throws only: `AiNotConfiguredException("The AI provider is not configured.")`, `AiTimeoutException("The AI provider did not respond in time.")`, `AiProviderException("The AI provider returned an error.")`, `AiInvalidOutputException("The AI response was not valid.")`. No provider body, ticket text, URL or key is placed in a message; inner exceptions are attached only for `HttpRequestException`/`TaskCanceledException`, never response content.
- U1's handler is adjusted (small change in U2's plan) to log AI failures as `LogWarning("AI failure {Type} TraceId {TraceId}", ex.GetType().Name, traceId)` — no exception object, so no stack or inner messages reach the log.

## Untrusted Output (NFR4.1, NFR4.2)

- Envelope parsed with System.Text.Json into wire DTOs (`choices[0].message.content`, `finish_reason`); content parsed with `JsonDocument` reading only `category`, `priority`, `sentiment`, `summary`, `suggestedAction` (BR8.4).
- `AiOutputValidator` normalises enum text (case-insensitive, strips spaces/`-`/`_`), rejects numbers and unknown names, trims and length-checks strings, rejects `finish_reason = "length"`.
- Validated values are mapped field by field onto the Ticket; there is no code path from AI text to Status, SQL or execution (BR8.5).
- Prompt: system message holds instructions, the JSON schema and allowed values; the user message holds ticket data as a JSON object (`{"title": "...", "description": "..."}`) serialised with System.Text.Json, so quotes and delimiters inside ticket text are escaped and cannot close the data block. The system message says the ticket content is data, not instructions.

## Endpoint Config (NFR4.3)

`AiOptions` validated at startup with `ValidateOnStart`: `BaseUrl` must be an absolute `https` URI; `Model` non-empty; `TimeoutSeconds` 1–120. The key is not validated at startup (empty = not configured → 503 per request).

## Data Minimisation (NFR5.1) and No Sending (NFR2.17)

Prompt builders accept only title, description and (for drafts) category, sentiment, summary, suggested action. No SMTP/webhook/messaging client exists anywhere in the project.
