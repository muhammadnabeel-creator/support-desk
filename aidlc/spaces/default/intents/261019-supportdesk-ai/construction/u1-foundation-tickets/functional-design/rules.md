# Business Rules — U1 Foundation & Tickets

Sources: FR1–FR6, FR10 and NFR1–NFR2 in `inception/requirements-analysis/requirements.md`; U1's stories (US1.1–US1.5, US4.1) via `inception/units-generation/unit-of-work-story-map.md`; contracts C1 and C3 in `inception/contract-design/contract-summary.md`; error-handling decision ADR-005 behind `inception/domain-design/components.md`.

```yaml
rules:
  - id: BR1.1
    statement: A ticket title is required and must be 3-200 characters after trimming surrounding whitespace.
    category: validation
    applies_to: create, update
    trigger: create or update request received
    logic: IF title is missing, null, or shorter than 3 / longer than 200 characters after trimming THEN reject
    violation: 400 ProblemDetails with an error on "title" naming the limit; nothing saved
    source: FR2.4, FR5.1
  - id: BR1.2
    statement: A ticket description is required and must be 10-4000 characters after trimming.
    category: validation
    applies_to: create, update
    trigger: create or update request received
    logic: IF description is missing, null, or outside 10-4000 characters after trimming THEN reject
    violation: 400 ProblemDetails with an error on "description"; nothing saved
    source: FR2.4, FR5.1
  - id: BR1.3
    statement: Title and description are stored trimmed.
    category: policy
    applies_to: create, update
    trigger: valid create or update
    logic: Store the trimmed text, not the raw input
    violation: n/a
    source: FR2.4
  - id: BR1.4
    statement: A new ticket starts Open, Medium priority, with all AI and draft fields empty and createdAt = updatedAt = now (UTC).
    category: policy
    applies_to: create
    trigger: valid create
    logic: Set defaults; ignore any other fields in the request
    violation: n/a
    source: FR2.2
  - id: BR1.5
    statement: Created tickets are saved durably and survive an application restart.
    category: constraint
    applies_to: create, update, status change
    trigger: any successful write
    logic: Commit the change to the SQLite database file before responding
    violation: n/a (write failure is an unexpected error, see BR6.2)
    source: FR1.6
  - id: BR2.1
    statement: Ticket lists are ordered newest createdAt first; ties are ordered by id, highest first.
    category: policy
    applies_to: list
    trigger: list request
    logic: Sort by createdAt descending, then id descending
    violation: n/a
    source: FR3.1
  - id: BR2.2
    statement: Optional status, priority and category filters are combined with AND and matched case-insensitively against enum names.
    category: policy
    applies_to: list
    trigger: list request with filters
    logic: For each filter present, keep only tickets whose field equals the parsed enum value; absent filters do not restrict
    violation: n/a
    source: FR3.2
  - id: BR2.3
    statement: An unknown filter value is rejected.
    category: validation
    applies_to: list
    trigger: list request with a filter value that is not an enum name
    logic: IF a filter value cannot be parsed to its enum THEN reject
    violation: 400 ProblemDetails naming the parameter and listing allowed values
    source: FR3.2
  - id: BR2.4
    statement: An empty result is a normal response.
    category: policy
    applies_to: list
    trigger: no tickets match
    logic: Return 200 with an empty array
    violation: n/a
    source: FR3 (AC3.1)
  - id: BR3.1
    statement: Operations on a ticket id that does not exist are refused as not found.
    category: constraint
    applies_to: get, update, status change (and U2's analyze, draft-reply)
    trigger: request names an id with no ticket
    logic: IF no ticket has this id THEN raise "ticket not found"
    violation: 404 ProblemDetails naming the id; nothing changed
    source: FR4, FR5, FR6
  - id: BR3.2
    statement: A ticket id in the URL must be a positive whole number.
    category: validation
    applies_to: every /api/tickets/{id} route
    trigger: request received
    logic: IF {id} is not an integer or is less than 1 THEN reject (do not let the router turn it into a 404)
    violation: 400 ProblemDetails naming "id"
    source: contract C1 shared conventions
  - id: BR3.3
    statement: Getting a ticket returns every field, including AI fields and the latest draft when present.
    category: policy
    applies_to: get
    trigger: get request for an existing id
    logic: Return the full ticket representation
    violation: n/a
    source: FR4.1
  - id: BR4.1
    statement: Update requires title, description and priority together, each valid.
    category: validation
    applies_to: update
    trigger: update request
    logic: IF any of the three is missing or invalid (BR1.1, BR1.2, priority not an enum name) THEN reject
    violation: 400 ProblemDetails naming each invalid field; ticket unchanged
    source: FR5.1
  - id: BR4.2
    statement: Update never changes status or AI/draft fields, even if the request includes them.
    category: constraint
    applies_to: update
    trigger: update request containing extra fields
    logic: Apply only title, description and priority; ignore all other fields
    violation: n/a
    source: FR5.2
  - id: BR4.3
    statement: Every successful change refreshes updatedAt to now (UTC); createdAt never changes.
    category: policy
    applies_to: update, status change (and U2's analyze, draft-reply)
    trigger: successful write to an existing ticket
    logic: Set updatedAt = now
    violation: n/a
    source: FR1.5
  - id: BR5.1
    statement: Any of the four statuses may be set from any status; setting the current status again succeeds.
    category: policy
    applies_to: status change
    trigger: status change request with a valid status
    logic: Set status to the requested value; no transition table is enforced
    violation: n/a
    source: FR6.2
  - id: BR5.2
    statement: A missing or unknown status value is rejected.
    category: validation
    applies_to: status change
    trigger: status change request
    logic: IF status is missing or not one of Open, InProgress, Resolved, Closed (case-insensitive) THEN reject
    violation: 400 ProblemDetails listing the allowed values; ticket unchanged
    source: FR6.1, FR6.2
  - id: BR6.1
    statement: Every error response uses the ProblemDetails shape; validation errors list each failing field.
    category: policy
    applies_to: all endpoints
    trigger: any 4xx/5xx response
    logic: Produce application/problem+json with type, title, status, detail and, for validation, errors per field
    violation: n/a
    source: FR10.1
  - id: BR6.2
    statement: Unexpected failures return a generic 500 with no stack trace, exception type, connection string or secret; details are logged server-side without secrets.
    category: policy
    applies_to: all endpoints
    trigger: unhandled exception
    logic: Catch in the global handler; log; return a generic ProblemDetails 500
    violation: n/a
    source: FR10.3, NFR2
  - id: BR6.3
    statement: A malformed or unreadable JSON request body is a client error, not a server error.
    category: validation
    applies_to: create, update, status change
    trigger: body cannot be parsed
    logic: Return 400 ProblemDetails
    violation: 400
    source: FR10.2
  - id: BR6.4
    statement: Typed failures map to fixed status codes in one place - ticket not found 404, invalid AI output 502, AI provider error 502, AI not configured 503, AI timeout 504.
    category: policy
    applies_to: global exception handler (failures raised by U1 and U2)
    trigger: typed failure raised
    logic: Look up the failure type and return its status code as ProblemDetails
    violation: n/a
    source: FR10.2, contract C3, ADR-005
  - id: BR6.5
    statement: Ticket operations in U1 never call the AI provider.
    category: constraint
    applies_to: create, list, get, update, status change
    trigger: any of these requests
    logic: No dependency on the AI service in these paths
    violation: n/a
    source: NFR1
```

## Rules Summary

| ID | Rule | On violation |
|---|---|---|
| BR1.1 | Title required, 3–200 chars trimmed | 400 |
| BR1.2 | Description required, 10–4000 chars trimmed | 400 |
| BR1.3 | Store trimmed text | — |
| BR1.4 | New ticket: Open, Medium, AI fields empty, timestamps now | — |
| BR1.5 | Saved durably, survives restart | — |
| BR2.1 | List newest first, ties by id descending | — |
| BR2.2 | Filters AND-combined, case-insensitive | — |
| BR2.3 | Unknown filter value | 400 listing allowed values |
| BR2.4 | No matches is 200 [] | — |
| BR3.1 | Unknown ticket id | 404 |
| BR3.2 | Id must be positive integer | 400 |
| BR3.3 | Get returns all fields | — |
| BR4.1 | Update needs title, description, priority | 400 |
| BR4.2 | Update ignores status/AI fields | — |
| BR4.3 | updatedAt refreshed on every change | — |
| BR5.1 | Any status to any status | — |
| BR5.2 | Missing/unknown status | 400 listing allowed values |
| BR6.1 | ProblemDetails everywhere | — |
| BR6.2 | Unexpected error: generic 500, no leaks | 500 |
| BR6.3 | Malformed JSON body | 400 |
| BR6.4 | Typed failure → status code table | 404/502/503/504 |
| BR6.5 | No AI calls in ticket operations | — |
