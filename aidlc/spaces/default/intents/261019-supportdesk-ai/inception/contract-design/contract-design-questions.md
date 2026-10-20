# Contract Design — Questions

Inputs: units and dependency graph (`inception/units-generation/unit-of-work.md`, `unit-of-work-dependency.md`), components (`inception/domain-design/components.md`), requirements (`inception/requirements-analysis/requirements.md`).

Boundaries to pin down: the public REST API (consumed by Swagger, `.http` files and the reviewer), the in-process `IAiTicketService` contract between the Tickets flows and AiAssistant, and the outbound call to Groq. Error codes (400/404/502/503/504/500 as ProblemDetails) are already decided.

### Q1. How should Category values look in JSON?
The requirements show "Account Access" (with a space) but filters use enum names like `AccountAccess`; the requirements review flagged this as ambiguous.

A. (Suggested) Responses use the enum name everywhere (`"AccountAccess"`, `"TechnicalIssue"`) like every other enum; requests and filters accept the enum name case-insensitively; only the AI may answer in display form ("Account Access"), which the validator converts
B. Responses use display text (`"Account Access"`); requests and filters accept both forms
X. Other (please specify)

[Answer]: A

### Q2. Should the API be versioned?
A. (Suggested) No version in the URL (`/api/tickets`, as in the brief); changes after submission must be additive only (new optional fields, never renamed or removed ones)
B. Version in the URL (`/api/v1/tickets`)
X. Other (please specify)

[Answer]: A

### Q3. What should `POST /api/tickets/{id}/analyze` return?
The brief's example shows just the five analysis fields.

A. (Suggested) The five analysis fields exactly as in the brief (`category`, `priority`, `sentiment`, `summary`, `suggestedAction`) plus `previousPriority` (so the overwrite is visible) and the updated `ticket`
B. Only the five analysis fields, exactly like the brief's example
C. Only the updated ticket
X. Other (please specify)

[Answer]: A
