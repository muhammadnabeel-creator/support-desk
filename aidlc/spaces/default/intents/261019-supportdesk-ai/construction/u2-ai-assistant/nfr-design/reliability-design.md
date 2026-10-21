# Reliability Design — U2 AI Assistant

Sources: `construction/u2-ai-assistant/nfr-requirements/reliability-requirements.md` (NFR1.2, NFR1.3, NFR3.8–NFR3.11), `performance-requirements.md`, `security-requirements.md`, `tech-stack-decisions.md`; failure map in `construction/u2-ai-assistant/functional-design/functional-spec.md`; contract C4 (corrected) in `inception/contract-design/contract-summary.md`.

## Failure Classification (NFR3.8)

```
GroqAiTicketService.SendAsync(prompt, requestToken):
  if key blank                         -> throw AiNotConfiguredException          (503)
  linked = CreateLinkedTokenSource(requestToken); linked.CancelAfter(timeout)
  try   response = await http.SendAsync(message, linked.Token)
  catch OperationCanceledException when requestToken.IsCancellationRequested -> rethrow (client gone; 499 in U1 handler)
  catch OperationCanceledException     -> throw AiTimeoutException                (504)
  catch HttpRequestException           -> throw AiProviderException               (502)
  if !response.IsSuccessStatusCode     -> throw AiProviderException               (502)
  envelope missing choices/content     -> throw AiProviderException               (502)
  finish_reason == "length" or content invalid -> throw AiInvalidOutputException  (502)
```

## Design

| Requirement | Design |
|---|---|
| NFR1.2 | U1 services/controllers keep no reference to `IAiTicketService`; only the new analyze/draft paths use it. |
| NFR1.3 | Key checked per call (above); `AddHttpClient` registration never needs the key. |
| NFR3.9 | `TicketService.AnalyzeAsync/DraftReplyAsync`: load ticket (no tracking change yet) → await AI result → apply fields → single `SaveChangesAsync`. Any exception before apply means nothing is saved. |
| NFR3.10 | `AddOptions<AiOptions>().BindConfiguration("Ai").Validate(...).ValidateOnStart()`; messages name the key, e.g. "Ai:TimeoutSeconds must be between 1 and 120". |
| NFR3.11 | No retry handler. |

## Draft Input

For drafts the Tickets service passes the saved analysis only when the ticket has been analyzed (Category is set): category, sentiment, AiSummary and SuggestedAction from the ticket (current priority is not sent). If the ticket was edited after analysis, the saved analysis is still used; it is context for a draft that a human reviews.
