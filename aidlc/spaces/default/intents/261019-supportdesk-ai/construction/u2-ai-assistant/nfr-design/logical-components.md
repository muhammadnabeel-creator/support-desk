# Logical Components — U2 AI Assistant

Sources: `construction/u2-ai-assistant/nfr-requirements/tech-stack-decisions.md` (NFR8.15 layout) and the other U2 NFR requirement files; components in `inception/domain-design/components.md`; contracts C1–C4 in `inception/contract-design/contract-summary.md`; workflows in `construction/u2-ai-assistant/functional-design/functional-spec.md`.

| Component | Location | Responsibility | Failure domain |
|---|---|---|---|
| `IAiTicketService` | `src/SupportDesk.Api/Ai/` | Contract C2: `AnalyzeAsync`, `DraftReplyAsync` | — |
| `GroqAiTicketService` | `Ai/` | HTTP to Groq, timeout, failure classification, one log line | External (Groq) |
| `AiPrompts` | `Ai/` | System/user messages; ticket data as an escaped JSON object | — |
| `AiOutputValidator` | `Ai/` | Content → `AiAnalysisResult` / `AiDraftResult` or invalid-output | — |
| `AiOptions` | `Ai/` | Bound + validated config; key never printed | Startup |
| Groq wire DTOs | `Ai/Groq/` | Request/response envelope shapes | — |
| `AiAnalysisResult`, `AiDraftResult` | `Ai/` | Validated value types (contract C2) | — |
| Tickets AI flows | `Tickets/TicketService.cs` (+ `ITicketService`) | Load → AI → apply → single save | DB |
| Endpoints | `Tickets/TicketsController.cs` + `Tickets/Dtos/AnalysisResponse.cs`, `DraftReplyResponse.cs` | `POST /{id}/analyze`, `POST /{id}/draft-reply` | Per request |
| Error logging tweak | `Errors/GlobalExceptionHandler.cs` | AI failures logged without exception object | — |

Blast radius: a Groq outage, bad key or bad output affects only the two AI endpoints; CRUD and dashboard are unaffected.
