# Code Generation Plan — U2 AI Assistant

## Summary

- Builds: the Groq AI adapter (`IAiTicketService` → `GroqAiTicketService`) with timeout, failure classification and output validation, plus the two endpoints `POST /api/tickets/{id}/analyze` and `POST /api/tickets/{id}/draft-reply`
- Touches: new `src/SupportDesk.Api/Ai/` folder (and `Ai/Groq/`), `Tickets/ITicketService.cs`, `Tickets/TicketService.cs`, `Tickets/TicketsController.cs`, `Tickets/Dtos/` (2 new DTOs), `Errors/GlobalExceptionHandler.cs` (logging only), `Program.cs`, `appsettings.json`, `SupportDesk.Api.csproj` (UserSecretsId)
- Tests: deferred to Task F at your request (about 30 xUnit tests tagged `Unit=U2`, listed in `unit-test-instructions.md`); no new migration — U2 writes fields that already exist on `Ticket`

## Inputs

Design for this unit: `construction/u2-ai-assistant/functional-design/` (entities.md, rules.md BR7.1–BR11.2, functional-spec.md WF7–WF9 and the failure map), `nfr-requirements/` and `nfr-design/` (performance, security, reliability, observability, logical components). Contracts C1–C4 in `inception/contract-design/contract-summary.md` (C2 Groq only, C4 timeout 504, client disconnect 499). Unit scope in `inception/units-generation/unit-of-work.md`. Stories US2.1–US2.5. Decision 2026-10-20: Groq only, no mock AI.

## How the work is approved

As in Unit 1, the work is built in **4 tasks**. After each task I stop, show what was done, tick it in `docs/progress.md`, and wait for your approval before the next task. `dotnet build` stays green (0 warnings) after every task. I never commit; I give you the commit commands.

**Tests deferred (your request, 2026-10-20):** Tasks A–D build the feature code only. The test steps (2, 4, 6) move to the final **Task F — Tests**, run only when you say so, together with Unit 1's deferred tests. The 80% coverage floor still applies at Task F; nothing is dropped, only reordered.

## Steps

### Task A — AI building blocks and configuration

- [x] **Step 1 — Business logic (AI adapter parts): implement.** Folder `src/SupportDesk.Api/Ai/`:
  - `IAiTicketService.cs` — contract C2: `AnalyzeAsync(title, description, ct)` → `AiAnalysisResult`; `DraftReplyAsync(title, description, AiDraftContext? analysis, ct)` → `AiDraftResult`.
  - `AiAnalysisResult.cs` (Category, Priority, Sentiment, Summary, SuggestedAction), `AiDraftResult.cs` (Draft), `AiDraftContext.cs` (saved Category, Sentiment, Summary, SuggestedAction) — immutable records.
  - `AiOptions.cs` — `Ai:Groq:ApiKey`, `Ai:Groq:Model` (default `llama-3.3-70b-versatile`), `Ai:Groq:BaseUrl` (default `https://api.groq.com/openai/v1`), `Ai:TimeoutSeconds` (default 20); `ToString()` never prints the key; `IsConfigured` = key not blank.
  - `AiPrompts.cs` — system message = fixed instructions, the JSON schema and the allowed enum names; user message = ticket data serialized as an escaped JSON object (`{"title":…,"description":…}`), so ticket text cannot change the instructions (BR11.1, NFR4.2). Draft prompt: plain text, no markdown, under 2,000 characters, never promise actions; the saved analysis is added only when given (BR7.3).
  - `AiOutputValidator.cs` — analysis: parse as one JSON object (BR8.1); read only the five known fields (BR8.4); enum names matched case-insensitively ignoring spaces, hyphens and underscores, numbers rejected (BR8.2); summary/suggestedAction trimmed, 1–500 characters (BR8.3). Draft: trimmed, 1–4000 characters (BR9.1). Any failure throws `AiInvalidOutputException` with a fixed generic message.
  - `Ai/Groq/` wire DTOs: `GroqChatRequest`, `GroqMessage`, `GroqResponseFormat`, `GroqChatResponse`, `GroqChoice` (snake_case JSON names) (NFR8.12).
- [ ] **Step 2 — Business logic (AI adapter parts): write and run its tests.** *(Deferred to Task F.)*

### Task B — Groq client and registration

- [x] **Step 3 — Business logic (Groq adapter): implement.** `Ai/GroqAiTicketService.cs` as a typed `HttpClient` (`AddHttpClient<IAiTicketService, GroqAiTicketService>`; `BaseAddress` = BaseUrl with a trailing slash, relative path `chat/completions` so `/v1` is kept; `HttpClient.Timeout` infinite so our own timeout rules):
  - No key → `AiNotConfiguredException` before any network call (BR10.1).
  - Linked token source with `CancelAfter(TimeoutSeconds)` (BR10.2). Client cancel → rethrow (U1 handler gives 499); our timeout → `AiTimeoutException` (504).
  - `HttpRequestException`, non-2xx (including 429), missing `choices`/`message.content`, or a JSON/IO error while reading the body → `AiProviderException` (502) with a generic message (BR10.3). `finish_reason == "length"` or invalid content → `AiInvalidOutputException` (502).
  - Per-request `Authorization: Bearer` header (never a default header, NFR2.14). Analysis: `max_tokens` 400, `temperature` 0.2, `response_format: json_object`; draft: `max_tokens` 700, `temperature` 0.2 (NFR3.4). No retries (BR10.4).
  - One log line per call in a `finally`: `AI {Operation} via {Model} -> {Outcome} in {ElapsedMs} ms (HTTP {StatusCode})`, Information for ok, Warning otherwise; never the key, ticket text or AI text (BR10.5, NFR2.18).
  - `Program.cs`: `AddOptions<AiOptions>().BindConfiguration("Ai").Validate(...).ValidateOnStart()` (https BaseUrl, non-empty model, timeout 1–120; a missing key does **not** stop startup) and the typed client registration. `appsettings.json`: the `Ai` section with an empty key placeholder, and `"System.Net.Http.HttpClient": "Warning"` under Logging (NFR2.20). `SupportDesk.Api.csproj`: add a `UserSecretsId` (NFR8.14).
  - `Errors/GlobalExceptionHandler.cs`: AI failures logged as exception type + status code + TraceId, without the exception object (NFR2.19).
- [ ] **Step 4 — Business logic (Groq adapter): write and run its tests.** *(Deferred to Task F.)*

### Task C — Ticket AI flows and endpoints

- [x] **Step 5 — Business logic + API / endpoint: implement.**
  - `Tickets/ITicketService.cs` + `TicketService.cs`: `AnalyzeAsync(id, ct)` and `DraftReplyAsync(id, ct)` — load the ticket (404 before any AI call, BR7.2), call `IAiTicketService`, then apply and save once (BR7.4–BR7.6, BR9.2). Any status allowed (BR7.1); status never touched (BR8.5). Analysis remembers the previous priority. Draft passes the saved analysis only when `Category` is set. The U1 methods stay free of any AI reference (NFR1.2): the AI service is only used by the two new methods.
  - `Tickets/Dtos/AnalysisResponse.cs` (category, priority, sentiment, summary, suggestedAction, previousPriority, ticket) and `DraftReplyResponse.cs` (ticketId, draft, generatedAt, notice `Draft — requires human review; not sent`) per contract C1 (BR9.3).
  - `Tickets/TicketsController.cs`: `POST /api/tickets/{id}/analyze` and `POST /api/tickets/{id}/draft-reply`, same id validation as U1 (BR3.2), XML comments and `ProducesResponseType` for 200/400/404/502/503/504 so Swagger shows them. Nothing is sent anywhere (BR9.4).
- [ ] **Step 6 — Business logic + API / endpoint: write and run its tests.** *(Deferred to Task F.)*

### Task D — Configuration check, docs and format

- [x] **Step 7 — Environment/build configuration.** Run the API without a key: CRUD works and both AI endpoints return 503 (AC2.5.1, NFR1.3). Give you the commands to set your own key (`dotnet user-secrets set "Ai:Groq:ApiKey" "<your key>" --project src/SupportDesk.Api`) for your manual test; I never see or store the key.
- [x] **Step 8 — Documentation and traceability.** XML doc comments on the new public types and actions; `dotnet format`; `dotnet build && dotnet test` green; `dotnet format --verify-no-changes` clean. Write `code-summary.md`, `traceability.json` and the source manifest. README AI section with the demo-data warning (BR11.2, NFR5.2) is delivered in U4.

### Task F — Tests (deferred; only when you say)

Steps 2, 4 and 6 above, run with Unit 1's deferred tests, plus the 80% line-coverage check (`dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"`).

## Story-to-Step Traceability

| Story | Steps |
|---|---|
| US2.1 Analyze a ticket | 1, 3, 5 (tests 2, 4, 6) |
| US2.2 Reject invalid AI output | 1, 3 (tests 2, 4) |
| US2.3 AI failures handled safely | 3, 5, 7 (tests 4, 6) |
| US2.4 Draft a reply (never sent) | 1, 3, 5 (tests 2, 4, 6) |
| US2.5 Connect the real Groq AI | 1, 3, 7 (tests 4) |

## Testing Contract

```json
{
  "version": 1,
  "methodology": "test-after",
  "source": "team",
  "ordering": "For each unit, we implement each testable layer (DTO validation, then services, then controllers) and then write and run that layer's tests before we move to the next layer or merge the unit into `main`.",
  "scope": "classic",
  "test_strategy": "standard",
  "project_type": "greenfield",
  "applicable_notes": [
    {
      "layer": "org",
      "text": "We treat tests as a first-class deliverable in every Bolt. The specific\nmethodology (TDD, BDD, ATDD, or classic test-after) is affirmed at\npractices-discovery and recorded in `team.md` under this heading with explicit\n`Methodology` and `Ordering` fields; Code Generation resolves those fields\nindependently from coverage, tooling, and scope notes.\n\nWhen no posture has been affirmed, our default per scope is:\n- **Methodology**: test-after\n- **Ordering**: implement each applicable testable layer, then write and run\n  that layer's tests.\n- `mvp`, `enterprise`, `feature`, `infra`, `classic` add an 80% line-coverage\n  floor and CI execution before merge.\n- `bugfix`, `security-patch` add a targeted regression for the specific\n  bug/vulnerability and require the existing suite to remain green.\n- `express` uses the Minimal strategy: requirement-driven unit tests (one per\n  requirement, with a happy-path floor per component); existing tests remain\n  green.\n- `poc`, `refactor`, `workshop` add no extra new-test floor and require the\n  existing suite to remain green.\n\nThe active `Test Strategy` still applies in every scope and determines test\nvolume/types. Scope floors are additive; they never reduce or replace the\nselected strategy.\n\nBuild and Test verifies defined coverage floors and affirmed quality targets;\nthey may not be weakened to make a step pass.\n\nAffirm a stricter posture in `team.md` if the team commits to one."
    },
    {
      "layer": "team",
      "text": "- **Methodology**: test-after\n- **Methodology evidence**: The trainee chose it in the interview (Q4, option A).\n  It suits a solo developer learning C# and ASP.NET Core under a 5-day deadline,\n  and the brief asks for tests of the key business rules and AI-output\n  validation without requiring tests to be written first.\n- **Ordering**: For each unit, we implement each testable layer (DTO validation,\n  then services, then controllers) and then write and run that layer's tests\n  before we move to the next layer or merge the unit into `main`.\n- **Framework**: xUnit with the built-in `Assert`, in a separate test project in\n  the solution. We do not use extra assertion or mocking libraries.\n- **Fakes**: hand-written fakes for `IAiTicketService` and other interfaces.\n  Automated tests never call a real AI provider. Canned fake responses cover:\n  - valid output;\n  - malformed JSON;\n  - out-of-range enum values;\n  - a timeout;\n  - provider errors.\n- **Database tests**: service tests use EF Core against SQLite in-memory mode\n  (`DataSource=:memory:` with a connection held open), not the EF Core InMemory\n  provider.\n- **API tests**: a few `WebApplicationFactory` tests cover the main endpoints.\n  Each test replaces `IAiTicketService` with a fake and uses its own SQLite\n  in-memory database.\n- **Behaviour we always cover**:\n  - ticket validation (required fields, length limits);\n  - status values (only Open, InProgress, Resolved and Closed are accepted);\n  - 404 for unknown IDs;\n  - AI-output validation: typed DTO and allowed values, and malformed output is\n    not saved;\n  - AI timeouts and provider failures return a controlled error;\n  - draft-only replies;\n  - dashboard counts.\n- **Coverage floor**: at least 80% line coverage, measured with coverlet\n  (`dotnet test --collect:\"XPlat Code Coverage\"`) on hand-written code only.\n  EF Core migrations and `Program.cs` are excluded from measurement. We do not\n  lower this floor to make a step pass.\n- **Checks before merge (in place of CI)**: there is no CI service. Before a unit\n  branch is merged into `main` and before every push, `dotnet build` and\n  `dotnet test` must pass locally, and `dotnet format --verify-no-changes` must\n  report no changes. We never merge or push a failing build.\n- **Test strategy**: Standard (from the scope). The notes above add to it and\n  never replace it."
    },
    {
      "layer": "project",
      "text": "- Keep the 80% line-coverage floor, measured on hand-written code only (EF migrations and Program.cs excluded); with no CI in scope, a local dotnet build + dotnet test + dotnet format --verify-no-changes run stands in for CI before every merge and push (escalated: knowingly departs from the org CI-before-merge default). (learned 2026-10-19)"
    }
  ],
  "obligations": {
    "strategy": "standard",
    "strategy_volume": [
      "Five to eight tests per component.",
      "Unit tests plus integration tests for key boundaries.",
      "Add E2E, performance, or security tests when requirements demand them."
    ],
    "scope_floor": [
      "Keep the existing test suite green.",
      "This scope adds no extra new-test floor beyond the selected test strategy."
    ],
    "combination_rule": "Apply every selected-strategy obligation and every scope-floor obligation; neither replaces the other, and a targeted scope regression may add the narrowest necessary test type beyond the strategy default."
  },
  "plan_profile": {
    "methodology": "test-after",
    "runner_step": "Bootstrap the minimal test runner/configuration and record the exact unit-scoped command.",
    "runner_ready_before_first_test": true,
    "testable_layers": [
      "Data model / database behavior",
      "Repository / data access",
      "Business logic",
      "API / endpoint",
      "Frontend behavior"
    ],
    "steps": [
      "Project structure and production configuration skeleton.",
      "Bootstrap the minimal test runner/configuration and record the exact unit-scoped command.",
      "Data model / database behavior - implement.",
      "Data model / database behavior - write and run its tests after implementation.",
      "Repository / data access - implement.",
      "Repository / data access - write and run its tests after implementation.",
      "Business logic - implement.",
      "Business logic - write and run its tests after implementation.",
      "API / endpoint - implement.",
      "API / endpoint - write and run its tests after implementation.",
      "Frontend behavior - implement.",
      "Frontend behavior - write and run its tests after implementation.",
      "Environment/build configuration.",
      "Documentation and traceability."
    ]
  },
  "input_sha256": "sha256:95d2aef0dfdaf46cfe7ccac2228e04bdeabb99913f63bbb467ab73485bd46aab",
  "contract_sha256": "sha256:69e76c0c6539fd16cf5295d62551b49883346a948a0f4c59df88fab6fae6301e"
}
```

Layer notes: the project structure and the test runner already exist from U1 (Task A there), and `dotnet test tests/SupportDesk.Api.Tests --filter "Unit=U2"` is the unit-scoped command. "Data model / database behavior" and "Repository / data access" do not apply: U2 adds no entity, column or migration and services use the DbContext directly (ADR-004). "Frontend behavior" does not apply (API only).
