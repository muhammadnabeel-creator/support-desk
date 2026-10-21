# Entities — U2 AI Assistant

Sources: unit U2 in `inception/units-generation/unit-of-work.md` and its stories in `unit-of-work-story-map.md` (US2.1–US2.5); FR7, FR8, FR11 in `inception/requirements-analysis/requirements.md` (FR11 updated 2026-10-20: Groq only); AiAssistant and Tickets in `inception/domain-design/components.md`; contracts C2–C4 in `inception/contract-design/contract-summary.md`; answers in `functional-design-questions.md`.

U2 adds no new persisted entity. It writes AI fields on the existing **Ticket** (owned by Tickets, U1) and uses transient value types that never reach the database directly.

```yaml
entities:
  - name: Ticket
    description: Existing entity from U1. U2 writes only the fields below, through the Tickets service.
    attributes:
      - { name: category, type: enum, required: false, allowed: [AccountAccess, Billing, TechnicalIssue, FeatureRequest, GeneralInquiry, Other], constraints: "set by analyze" }
      - { name: sentiment, type: enum, required: false, allowed: [Positive, Neutral, Negative, Frustrated], constraints: "set by analyze" }
      - { name: aiSummary, type: text, required: false, min: 1, max: 500, constraints: "set by analyze" }
      - { name: suggestedAction, type: text, required: false, min: 1, max: 500, constraints: "set by analyze" }
      - { name: priority, type: enum, required: true, allowed: [Low, Medium, High, Critical], constraints: "overwritten by analyze" }
      - { name: draftReply, type: text, required: false, min: 1, max: 4000, constraints: "set by draft-reply; never sent" }
      - { name: draftReplyGeneratedAt, type: timestamp-utc, required: false, constraints: "set with draftReply" }
      - { name: updatedAt, type: timestamp-utc, required: true, constraints: "refreshed on every successful analyze or draft" }
    relationships: []

value_types:
  - name: AiAnalysisResult
    description: Validated output of an analysis; produced only by AiAssistant's validator.
    attributes:
      - { name: category, type: enum, required: true, allowed: [AccountAccess, Billing, TechnicalIssue, FeatureRequest, GeneralInquiry, Other] }
      - { name: priority, type: enum, required: true, allowed: [Low, Medium, High, Critical] }
      - { name: sentiment, type: enum, required: true, allowed: [Positive, Neutral, Negative, Frustrated] }
      - { name: summary, type: text, required: true, min: 1, max: 500, constraints: "trimmed; not whitespace-only" }
      - { name: suggestedAction, type: text, required: true, min: 1, max: 500, constraints: "trimmed; not whitespace-only" }
  - name: AiDraftResult
    description: Validated reply draft.
    attributes:
      - { name: draft, type: text, required: true, min: 1, max: 4000, constraints: "trimmed; not whitespace-only" }
  - name: AiOptions
    description: Configuration bound from the Ai section; the key never appears in committed files.
    attributes:
      - { name: groqApiKey, type: secret-text, required: false, constraints: "user secrets or environment only; missing = not configured" }
      - { name: groqModel, type: text, required: true, default: "openai/gpt-oss-120b" }
      - { name: groqBaseUrl, type: url, required: true, default: "https://api.groq.com/openai/v1" }
      - { name: timeoutSeconds, type: integer, required: true, default: 20, min: 1, max: 120 }
```

## Summary

No new tables or migration. Analysis writes Category, Sentiment, AiSummary, SuggestedAction and overwrites Priority; draft-reply writes DraftReply and DraftReplyGeneratedAt. Both refresh UpdatedAt. `AiAnalysisResult` and `AiDraftResult` are the only shapes AI data can take inside the app, and they exist only after validation. `AiOptions` carries Groq settings; the API key comes from user secrets or environment variables.
