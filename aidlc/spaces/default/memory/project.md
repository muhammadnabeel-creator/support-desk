# Project-Level Rules

> Project-specific specialisation and corrections. Loaded after `org.md` and
> `team.md` as strict-additive guidance; contradictions with broader policy
> are rejected. Populated by practices-discovery and the self-learning loop.
>
> Use sparingly: most teams don't need a project layer. Reach for it
> only when this specific project needs stable, durable guidance beyond the
> team practice (for example, package-specific release checks or an additional
> regression suite for a legacy component).

## Way of Working

<!-- Project-specific specialisation. Example: -->
<!-- This monorepo requires package-scoped branch names and a package owner -->
<!-- review in addition to the team's normal merge policy. -->

## Walking Skeleton

<!-- Project-specific specialisation. Example: -->
<!-- The walking skeleton must exercise the legacy service adapter as well -->
<!-- as the new service boundary. -->

## Testing Posture

<!-- Project-specific specialisation. -->

- Keep the 80% line-coverage floor, measured on hand-written code only (EF migrations and Program.cs excluded); with no CI in scope, a local dotnet build + dotnet test + dotnet format --verify-no-changes run stands in for CI before every merge and push (escalated: knowingly departs from the org CI-before-merge default). (learned 2026-10-19) <!-- cid:261019-supportdesk-ai:practices-discovery:8dad22a6e4aaffad1697e422d6f0ecca1df8d2b2e665af101f8caafe68ef7377 -->

## Guard Policy

<!-- Project-specific. Mode: strict, relaxed, or off. Strict here holds for every intent, whatever one person asks in chat; relaxed or off here applies to every intent whose policy came from its scope, unless the person sets another; changing this line changes it. A section under the retired Change Control heading, written by an earlier release, is still read. -->

## Deployment

<!-- Project-specific specialisation. -->

## Code Style

<!-- Project-specific specialisation. -->

## Tech Stack

<!-- Technology choices locked for this project. -->

## Decided

<!-- Decisions made in earlier stages that should not be re-asked. -->
<!-- Format: DECIDED: [decision] (Stage [slug], [date]) -->

## Scope Overrides

<!-- Custom scope rules for this project. -->

## Forbidden

<!-- Populated by practices-discovery affirmation gate. -->
<!-- Format: NEVER [behavior] (affirmed [date]) -->
<!-- Example: NEVER throw exceptions across service layer boundaries (affirmed 2026-05-17) -->

- NEVER commit API keys, passwords or other secrets; configuration comes from environment variables or user secrets, and only placeholder examples are committed. (affirmed 2026-10-19)

- NEVER send an AI-generated reply automatically; drafts are returned for human review only. (affirmed 2026-10-19)

- NEVER save or return AI output that has not been validated against a typed DTO and the allowed values (status and priority enums). (affirmed 2026-10-19)

- NEVER send real customer, confidential or personal data to an external AI provider. (affirmed 2026-10-19)

- NEVER execute AI-generated code, SQL or commands. (affirmed 2026-10-19)

## Mandated

<!-- Populated by practices-discovery affirmation gate. -->
<!-- Format: ALWAYS [behavior] (affirmed [date]) -->
<!-- Example: ALWAYS use Result<T,E> for fallible operations in service layer (affirmed 2026-05-17) -->

- ALWAYS keep core ticket operations working when the AI provider is unavailable, apply a timeout to every AI request, and never expose stack traces or secrets in error responses. (affirmed 2026-10-19)

## Corrections

<!-- Project-specific corrections from human feedback. -->
<!-- Format: NEVER/ALWAYS [behavior] (learned [date]) -->
- Treat org.md defaults as suggestions adapted to this solo, 5-day, local-only .NET trainee project: use .editorconfig + dotnet format instead of Prettier/ESLint examples, and push-to-GitHub plus a clean-clone check instead of deploy-on-merge to staging. (learned 2026-10-19) <!-- cid:261019-supportdesk-ai:practices-discovery:a2dd1105efc1061525e20f2b4bab05cf65bdf7a9b556e2b615de7c9708b7a4d5 -->
- Unit branches merge into main with a merge commit (no squash), keeping their commits visible, because the trainer's brief grades meaningful commits (escalated: knowingly departs from the org squash-merge default). (learned 2026-10-19) <!-- cid:261019-supportdesk-ai:practices-discovery:e72d087a277e0216b750d50bcf3d1edc889525696ec67d6aa80489dd965baf29 -->
- The AI provider is Groq (groq.com, OpenAI-compatible API at api.groq.com), not xAI's Grok; confirm similar-sounding provider names with a follow-up question rather than guessing. (learned 2026-10-19) <!-- cid:261019-supportdesk-ai:requirements-analysis:4ce890daee893705ddd64867266bb9effbe7bfb445c593766dd15ae164e74dd6 -->
- The latest AI draft reply is stored on the ticket (DraftReply, DraftReplyGeneratedAt) beyond the brief's suggested data model, because the user chose to keep it; it is still never sent. (learned 2026-10-19) <!-- cid:261019-supportdesk-ai:requirements-analysis:34b24b4861ccb1f1744330cd4476486e1b3b0a6bc8092740df19d475e165c34e -->
- A component that only reads another component's entity (e.g. Dashboard counting tickets) is modelled as a read-only depends_on edge to the owning component, never as shared ownership, so every entity keeps exactly one owner and the dependency graph stays acyclic. (learned 2026-10-19) <!-- cid:261019-supportdesk-ai:domain-design:093e9f8fa6202aaa5f5cfa71bd2a8eaed6831e4fd261835b01c237b7a8bda8ff -->
- Keep the shared fixed-value types (Status, Priority, Category, Sentiment enums) in one shared Domain folder that both Tickets and AiAssistant reference, and state that ownership explicitly in the design so the coupling is visible. (learned 2026-10-19) <!-- cid:261019-supportdesk-ai:domain-design:77cd12071deb1726300214efa21e500a442427f2238823339ed6724bfa2c0cff -->
- Units are cut by feature, not strictly by component: U2 carries the AiAssistant component plus the Tickets service's analyze/draft operations, because those flows cannot be tested without the AI and belong to the same story group (US2.x). (learned 2026-10-19) <!-- cid:261019-supportdesk-ai:units-generation:f106abbed633e563c168ddbfb52223d8e664e0585ec64968077589a7031440e6 -->
