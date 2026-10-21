# Business Rules — U2 AI Assistant

Sources: FR7, FR8, FR10, FR11 and NFR1, NFR3–NFR5 in `inception/requirements-analysis/requirements.md`; stories US2.1–US2.5 via `inception/units-generation/unit-of-work-story-map.md`; contracts C2–C4 in `inception/contract-design/contract-summary.md`; AiAssistant in `inception/domain-design/components.md` (ADR-002); hard rules in `aidlc/spaces/default/memory/project.md`; answers in `functional-design-questions.md`.

```yaml
rules:
  - id: BR7.1
    statement: Analyze and draft-reply work for a ticket in any status.
    category: policy
    applies_to: analyze, draft-reply
    trigger: request for an existing ticket
    logic: Do not check status before calling the AI
    violation: n/a
    source: AC2.1.6, AC2.4.1
  - id: BR7.2
    statement: An unknown ticket id is refused before the AI is called.
    category: constraint
    applies_to: analyze, draft-reply
    trigger: request names an id with no ticket
    logic: Load the ticket first; IF absent THEN raise ticket-not-found and never call the AI
    violation: 404
    source: AC2.1.4, BR3.1
  - id: BR7.3
    statement: Only the ticket's title and description (and, for drafts, its saved analysis) are sent to the AI; the ticket id and timestamps are never sent.
    category: policy
    applies_to: analyze, draft-reply
    trigger: building the AI request
    logic: Prompt input = title + description (+ category, sentiment, summary, suggestedAction when analyzed)
    violation: n/a
    source: contract C2, NFR5
  - id: BR7.4
    statement: A valid analysis saves Category, Sentiment, AiSummary, SuggestedAction, overwrites Priority, refreshes UpdatedAt, and returns the five fields plus previousPriority and the updated ticket.
    category: policy
    applies_to: analyze
    trigger: AiAssistant returns a validated AiAnalysisResult
    logic: Remember old priority; apply all five values; save once
    violation: n/a
    source: FR7.3, AC2.1.1, AC2.1.2, AC2.1.5, contract C1 AnalysisResponse
  - id: BR7.5
    statement: A new analysis replaces any earlier analysis completely.
    category: policy
    applies_to: analyze
    trigger: analyze on an already analyzed ticket
    logic: Overwrite all AI fields; no history kept
    violation: n/a
    source: FR7.5, AC2.1.3
  - id: BR7.6
    statement: Any AI failure leaves the ticket exactly as it was (no partial writes).
    category: constraint
    applies_to: analyze, draft-reply
    trigger: invalid output, provider error, timeout, not configured, or client cancel
    logic: Save only after a validated result exists; on failure do not call SaveChanges
    violation: n/a (error code per BR10.x)
    source: FR7.4, AC2.2.1-AC2.2.3, AC2.3.1, AC2.4.3, AC2.4.5
  - id: BR8.1
    statement: Analysis output must be a single JSON object; anything else is invalid output.
    category: validation
    applies_to: AI analysis output
    trigger: provider returns text
    logic: IF the text does not parse as a JSON object THEN invalid
    violation: 502 invalid AI output
    source: FR7.2, AC2.2.1
  - id: BR8.2
    statement: category, priority and sentiment must each be one of the allowed values, matched case-insensitively and ignoring spaces, hyphens and underscores.
    category: validation
    applies_to: AI analysis output
    trigger: parsing the JSON object
    logic: Normalise ("account access" -> AccountAccess); IF not an allowed name THEN invalid; numbers are invalid
    violation: 502 invalid AI output
    source: FR7.2, AC2.2.2, AC2.2.4, contract C1 enum convention
  - id: BR8.3
    statement: summary and suggestedAction are required, trimmed, 1-500 characters, not whitespace-only.
    category: validation
    applies_to: AI analysis output
    trigger: parsing the JSON object
    logic: IF missing, not a string, empty after trim, or longer than 500 THEN invalid
    violation: 502 invalid AI output
    source: FR7.2, AC2.2.3, AC2.2.6
  - id: BR8.4
    statement: Unknown extra fields in the AI JSON are ignored; they are never stored or executed.
    category: policy
    applies_to: AI analysis output
    trigger: parsing the JSON object
    logic: Read only the five known fields
    violation: n/a
    source: FR7.2, NFR4, AC2.2.5
  - id: BR8.5
    statement: AI output is data only - it is never executed, never used to build SQL, and can never change ticket status.
    category: constraint
    applies_to: analyze, draft-reply
    trigger: always
    logic: Validated values map only to the listed fields; status is not among them
    violation: n/a
    source: NFR4, hard rule (never execute AI output), AC2.2.5
  - id: BR9.1
    statement: A draft must be trimmed text of 1-4000 characters, not whitespace-only.
    category: validation
    applies_to: AI draft output
    trigger: provider returns text
    logic: IF empty after trim or longer than 4000 THEN invalid
    violation: 502 invalid AI output
    source: FR8.2, AC2.4.3
  - id: BR9.2
    statement: A valid draft is stored as DraftReply with DraftReplyGeneratedAt, replacing any earlier draft, and UpdatedAt is refreshed.
    category: policy
    applies_to: draft-reply
    trigger: validated AiDraftResult
    logic: Overwrite DraftReply; set DraftReplyGeneratedAt = UpdatedAt = now
    violation: n/a
    source: FR8.2, AC2.4.1, AC2.4.2
  - id: BR9.3
    statement: The draft response carries ticketId, draft, generatedAt and notice "Draft — requires human review; not sent".
    category: policy
    applies_to: draft-reply
    trigger: successful draft
    logic: Return DraftReplyResponse (contract C1)
    violation: n/a
    source: FR8.2, AC2.4.1, contract C1
  - id: BR9.4
    statement: The system has no way to send a reply; nothing is emailed, messaged or posted.
    category: constraint
    applies_to: draft-reply
    trigger: always
    logic: No sending integration exists in the code
    violation: n/a
    source: FR8.3, hard rule (never auto-send), AC2.4.4
  - id: BR10.1
    statement: If no Groq API key is configured, AI requests fail fast as "not configured" without any network call; the app still starts.
    category: constraint
    applies_to: analyze, draft-reply
    trigger: AI request with empty key
    logic: Check key before calling Groq
    violation: 503 AI provider not configured
    source: FR11.2, AC2.3.3, AC2.4.5, AC2.5.1
  - id: BR10.2
    statement: Every AI call has a timeout (default 20 s, configurable) and honours request cancellation; a timeout is reported as AI timeout.
    category: constraint
    applies_to: analyze, draft-reply
    trigger: AI call exceeds timeout
    logic: Cancel the HTTP call at the timeout; distinguish timeout from the client disconnecting
    violation: 504 (client disconnect is not a timeout and writes nothing)
    source: NFR3, AC2.3.1, AC2.4.5, contract C4
  - id: BR10.3
    statement: A provider HTTP error, network failure or unexpected response envelope is reported as a provider error without the provider's raw text or the key.
    category: constraint
    applies_to: analyze, draft-reply
    trigger: non-2xx status, network exception, missing choices/content
    logic: Wrap as provider error with a generic message; log status code only
    violation: 502 AI provider error
    source: FR10.2, NFR2, AC2.3.2
  - id: BR10.4
    statement: No automatic retries.
    category: policy
    applies_to: AI calls
    trigger: any failure
    logic: Fail once; the user may call again
    violation: n/a
    source: assumption A7
  - id: BR10.5
    statement: The API key, ticket text and AI responses are never written to logs or error responses.
    category: constraint
    applies_to: AiAssistant logging and errors
    trigger: always
    logic: Log only operation, model, status code and duration
    violation: n/a
    source: NFR2, hard rules
  - id: BR11.1
    statement: Prompts wrap ticket text as delimited data and instruct the model to return JSON only, following a fixed schema that lists the allowed values; ticket text cannot change the instructions.
    category: policy
    applies_to: prompt building
    trigger: building the AI request
    logic: System message = instructions + schema; user message = delimited ticket data
    violation: n/a
    source: NFR4, contract C4
  - id: BR11.2
    statement: Only demo or synthetic ticket text is to be used with Groq; the README says so.
    category: policy
    applies_to: documentation, usage
    trigger: n/a
    logic: README AI section warning (delivered in U4)
    violation: n/a
    source: NFR5, AC2.5.3
```

## Rules Summary

| ID | Rule | On violation |
|---|---|---|
| BR7.1 | AI works on any status | — |
| BR7.2 | Unknown id refused before calling AI | 404 |
| BR7.3 | Only title/description (+ analysis) sent | — |
| BR7.4 | Valid analysis saved; priority overwritten; previousPriority returned | — |
| BR7.5 | Re-analysis replaces old analysis | — |
| BR7.6 | AI failure leaves ticket unchanged | per BR10 |
| BR8.1 | Output must be a JSON object | 502 |
| BR8.2 | Enum values must be allowed names (tolerant matching) | 502 |
| BR8.3 | summary/suggestedAction 1–500 chars | 502 |
| BR8.4 | Extra AI fields ignored | — |
| BR8.5 | AI output never executed, never sets status | — |
| BR9.1 | Draft 1–4000 chars | 502 |
| BR9.2 | Draft stored, replaces old one | — |
| BR9.3 | Draft response shape with "not sent" notice | — |
| BR9.4 | No sending capability | — |
| BR10.1 | No key → not configured | 503 |
| BR10.2 | Timeout (20 s default) | 504 |
| BR10.3 | Provider/network error, no raw text leaked | 502 |
| BR10.4 | No retries | — |
| BR10.5 | Key, ticket text, AI text never logged | — |
| BR11.1 | Prompt treats ticket text as data, JSON only | — |
| BR11.2 | Demo data only with Groq (README) | — |
