## Review

**Verdict:** READY
**Reviewer:** aidlc-architecture-reviewer-agent
**Date:** 2026-10-20T11:39:48Z
**Iteration:** 1

### Findings

| ID | Severity | Location | Finding | Required action | Status |
|---|---|---|---|---|---|
| R-01 | Minor | src/SupportDesk.Api/Tickets/Dtos/CreateTicketRequest.cs and UpdateTicketRequest.cs, `[Required]` attributes, together with TicketService.ValidateText | The `[Required]` checks run before the service. When one field is missing and another is too short, only the missing-field error comes back. Running the app, `POST /api/tickets` with `{"title":"ab"}` returned errors for `description` only, with no title-length error. BR4.1 says the 400 names each invalid field. | Either drop `[Required]` on the string fields so the service reports every failing field in one pass, or accept the two-step behaviour and note it in the spec. | New |
| R-02 | Minor | src/SupportDesk.Api/Program.cs, `AddSwaggerGen` and the DTO and enum type names | The generated OpenAPI does not match C1. Schemas are named `TicketStatus`, `TicketPriority`, `TicketCategory`, `TicketSentiment` and `TicketResponse`, where C1 uses `Status`, `Priority`, `Category`, `Sentiment` and `Ticket`. `title` and `minLength` are shown as 1 instead of 3 and 10. Request schemas show `additionalProperties:false` although extra fields are ignored. Success responses list `text/plain` and `text/json`. Contract-summary says the runtime OpenAPI must match C1 and U4 checks this at the release gate. | Add a schema-id selector and `[Produces("application/json")]` on the controller, and either set the length hints through `StringLength` or accept the differences as known. Record the decision before the U4 release gate. | New |
| R-03 | Minor | src/SupportDesk.Api/Program.cs, `UseExceptionHandler` and routing | 404 for an unknown route (`/api/nothing`) and 405 for `DELETE /api/tickets/1` return an empty body, not ProblemDetails. BR6.1 says every 4xx/5xx uses ProblemDetails. Only `/api/tickets/...` paths that reach MVC get the ProblemDetails body. | Add `app.UseStatusCodePages()` (it writes ProblemDetails now that `AddProblemDetails` is registered), or scope BR6.1 to controller-routed requests in the spec. | New |
| R-04 | Minor | src/SupportDesk.Api/Tickets/TicketsController.cs, `ListAsync` and `ParseFilter`, plus TicketService.ValidateFilter | The service re-checks that filter enums are defined even though the controller already rejects bad names with the same message pattern. The check cannot trigger from HTTP and duplicates the allowed-values text. | Keep the check for direct service callers, or remove it. Optional, not blocking. | New |
| R-05 | Suggestion | src/SupportDesk.Api/Errors/GlobalExceptionHandler.cs, `AiInvalidOutputException`/`AiProvider...` branches | The AI exceptions are mapped and logged at Warning with `exception` attached. U2 must keep exception messages free of provider text and secrets, since the stack and message go to the log. The doc comments already state this. | Carry the "no secrets or raw provider text in messages" rule into U2's design and review. | New |

### Validation Tool Results

| Tool | Result | Interpretation |
|---|---|---|
| `dotnet build` | PASS, 0 warnings, 0 errors | The solution builds. CS1591 is on and clean. |
| `dotnet test tests/SupportDesk.Api.Tests --filter "Unit=U1"` | PASS, 18 passed, 0 failed | The existing persistence, DB-path and smoke tests pass. |
| `dotnet ef database update` against a scratch SQLite file, then run with `Development` | PASS | The documented migration step works from a clean database. The app starts, and the DB folder is created on demand. |
| Manual HTTP probes against the running app (16 requests, scratch DB) | PASS | See the notes below. |

Probe results by rule:
- **BR1.1, BR1.2, BR1.3, BR1.4:** Create trims title and description. Status and priority in the body are ignored, giving Open and Medium. A 250-character title returned 400 on `title`. `createdAt` equals `updatedAt` and ends in `Z`.
- **BR2.2, BR2.3:** `?priority=zzz` returned 400 listing the allowed values. `status=open` matched case-insensitively.
- **BR3.1, BR3.2:** A missing id returned 404 with a ProblemDetails body naming the id. `/0` and an int-overflow id returned 400 on `id`.
- **BR4.1, BR4.2, BR4.3:** A short description on PUT returned 400. PATCH moved `updatedAt` forward and left `createdAt` unchanged.
- **BR5.1, BR5.2:** `"closed"` was accepted. `"1"` returned 400 listing the allowed values.
- **BR6.1, BR6.3:** `null`, `{bad`, `[]` and a wrong content type all returned ProblemDetails (400 or 415) with `traceId`. No stack trace, CLR type name or parser message appeared in any body.
- **BR6.4:** Handler mapping read from code: 404, 502, 502, 503, 504, generic 500. All five typed exceptions sit in `Errors/`, as C3 requires.
- **BR6.5:** Neither the controller nor the service references any AI type, and no AI configuration is registered.
- **BR1.5:** Persistence is covered by the existing `TicketPersistenceTests`. I did not test restart end to end by hand.

### Summary

The U1 code conforms to C1 and C3 on paths, status codes, string enum names, `Z` timestamps and ProblemDetails shapes. All 22 business rules traced to code or probed live, with no Critical or Major gaps. Controllers are thin, services are DI-registered, and `CancellationToken` flows through. There are no secrets, and nothing leaks a stack trace. The four Minor items concern a partial multi-field validation report, differences between generated OpenAPI and C1 naming, empty-body 404 and 405 responses, and a duplicated filter check. None blocks the unit.
