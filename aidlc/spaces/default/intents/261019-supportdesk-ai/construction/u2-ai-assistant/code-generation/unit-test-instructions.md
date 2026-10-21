# Unit Test Instructions — U2 AI Assistant

These tests are written in **Task F** (deferred at your request); this file records what they will be and how to run them.

## Framework and Setup

- Test project: `tests/SupportDesk.Api.Tests` (from U1; xUnit, built-in `Assert`, no extra assertion or mocking libraries).
- No new packages: `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.Sqlite` and `coverlet.collector` are already referenced.
- Every U2 test class carries `[Trait("Unit", "U2")]`.
- Automated tests **never call the real Groq API** and never need an API key.

## How to Run This Unit's Tests

```bash
dotnet test tests/SupportDesk.Api.Tests --filter "Unit=U2"
```

With coverage (Migrations and `Program.cs` excluded by `coverlet.runsettings`):

```bash
dotnet test tests/SupportDesk.Api.Tests --filter "Unit=U2" --settings coverlet.runsettings --collect:"XPlat Code Coverage"
```

## Coverage Target

At least 80% line coverage of U2's hand-written code (`Ai/`, `Ai/Groq/`, the new `TicketService` and controller methods, the two DTOs). The threshold is never lowered to pass; a shortfall is reported.

## Test Volume (Standard strategy: 5–8 per component, plus key integration boundaries)

| Component | File | ~Tests |
|---|---|---|
| AiOutputValidator | `Ai/AiOutputValidatorTests.cs` | 8 |
| AiPrompts + AiOptions | `Ai/AiPromptsTests.cs`, `Ai/AiOptionsTests.cs` | 5 |
| GroqAiTicketService | `Ai/GroqAiTicketServiceTests.cs` | 8 |
| TicketService AI flows | `Tickets/TicketServiceAiTests.cs` | 6 |
| API (end to end) | `Api/AiEndpointsApiTests.cs` | 6 |

What they check:

- **Validator:** valid analysis; not JSON / a JSON array; unknown or numeric enum; tolerant enum matching ("account access", "Technical-Issue"); missing, blank or 501-character summary; extra fields ignored; draft blank or longer than 4000.
- **Prompts/options:** ticket text with quotes and "ignore previous instructions" stays inside the escaped JSON data; ticket id and timestamps never in the prompt; `ToString()` hides the key; option validation (http BaseUrl, timeout 0 or 121 fail).
- **Groq adapter:** no key → not-configured with zero HTTP calls; 200 valid → result; 500 and 429 → provider error; missing `choices` → provider error; `finish_reason: length` → invalid output; slow handler past the timeout → AI timeout; caller cancellation → `OperationCanceledException` (not a timeout); request URL is `…/openai/v1/chat/completions` with a Bearer header.
- **TicketService:** analysis saves the five fields and returns the previous priority; re-analysis replaces; any status allowed and status unchanged; each AI failure leaves the ticket unchanged; unknown id → 404 with zero AI calls; draft saved with timestamp and saved analysis passed only when analyzed.
- **API:** analyze 200 shape; draft 200 with the "not sent" notice; 404; 503 / 504 / 502 bodies are ProblemDetails with `traceId` and no stack trace or provider text.

## Mocking and Stubbing

- Hand-written fakes only:
  - `FakeAiTicketService` with canned outcomes: valid result, invalid output, timeout, provider error, not configured; counts calls.
  - `StubHttpMessageHandler` returning canned Groq envelopes (valid output, malformed JSON, out-of-range enum, HTTP 500/429) or delaying past the timeout; records the request.
  - `FakeClock` from U1.
- Service tests: SQLite in-memory (`DataSource=:memory:` with an open connection).
- API tests: the U1 `WebApplicationFactory` setup, replacing `IAiTicketService` with the fake and using its own SQLite in-memory database.

## Test Data

- Synthetic demo tickets only (no real customer data).
- Each test builds its own tickets; no shared state.
- Length boundaries use generated strings (`new string('a', 501)`).
