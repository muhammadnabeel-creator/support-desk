# Entities — U1 Foundation & Tickets

Sources: unit U1 in `inception/units-generation/unit-of-work.md` and its stories in `unit-of-work-story-map.md` (US1.1–US1.5, US4.1); data model FR1 in `inception/requirements-analysis/requirements.md`; Ticket ownership in `inception/domain-design/components.md` (owned by Tickets); shapes and shared types in `inception/contract-design/contract-summary.md` (C1, C3).

```yaml
entities:
  - name: Ticket
    description: A customer support request. Owned by the Tickets component; the only persisted entity.
    attributes:
      - { name: id, type: integer, required: true, unique: true, constraints: "system-generated, positive, never reused" }
      - { name: title, type: text, required: true, min: 3, max: 200, constraints: "stored trimmed" }
      - { name: description, type: text, required: true, min: 10, max: 4000, constraints: "stored trimmed" }
      - { name: status, type: enum, required: true, allowed: [Open, InProgress, Resolved, Closed], default: Open }
      - { name: priority, type: enum, required: true, allowed: [Low, Medium, High, Critical], default: Medium }
      - { name: category, type: enum, required: false, allowed: [AccountAccess, Billing, TechnicalIssue, FeatureRequest, GeneralInquiry, Other], default: null, constraints: "set only by AI analysis (U2)" }
      - { name: sentiment, type: enum, required: false, allowed: [Positive, Neutral, Negative, Frustrated], default: null, constraints: "set only by AI analysis (U2)" }
      - { name: aiSummary, type: text, required: false, max: 500, default: null, constraints: "set only by AI analysis (U2)" }
      - { name: suggestedAction, type: text, required: false, max: 500, default: null, constraints: "set only by AI analysis (U2)" }
      - { name: draftReply, type: text, required: false, max: 4000, default: null, constraints: "set only by draft-reply (U2); never sent" }
      - { name: draftReplyGeneratedAt, type: timestamp-utc, required: false, default: null, constraints: "set with draftReply" }
      - { name: createdAt, type: timestamp-utc, required: true, constraints: "set once on creation, never changed" }
      - { name: updatedAt, type: timestamp-utc, required: true, constraints: "set on creation and on every change; never earlier than createdAt" }
    entity_constraints:
      - "All AI fields (category, sentiment, aiSummary, suggestedAction) are either all null (never analyzed) or all set (analyzed)."
      - "draftReply and draftReplyGeneratedAt are both null or both set."
      - "Enums are stored and transmitted by name, never by number."
    relationships: []

shared_value_types:   # live in U1's shared Domain folder; referenced by U2 and U3 (contract C3)
  - { name: TicketStatus, values: [Open, InProgress, Resolved, Closed] }
  - { name: TicketPriority, values: [Low, Medium, High, Critical] }
  - { name: TicketCategory, values: [AccountAccess, Billing, TechnicalIssue, FeatureRequest, GeneralInquiry, Other] }
  - { name: TicketSentiment, values: [Positive, Neutral, Negative, Frustrated] }
```

## Summary

U1 owns a single entity, **Ticket**, with 13 attributes: identity and timestamps, the two user-entered text fields, two lifecycle enums (status, priority), and six AI fields that stay empty until Unit 2 fills them. The four fixed-value types live in one shared Domain folder so U2 and U3 use the same definitions. There are no relationships; tickets stand alone.
