# Story Map by Unit — SupportDesk AI

Stories from `inception/user-stories/stories.md`; units from `unit-of-work.md`. One row per story.

## Story Assignments

| Story | Title | Unit ID | Directory | Order in unit |
|---|---|---|---|---|
| US1.1 | Create a ticket | U1 | u1-foundation-tickets | 1 |
| US1.3 | View one ticket | U1 | u1-foundation-tickets | 2 |
| US1.2 | List and filter tickets | U1 | u1-foundation-tickets | 3 |
| US1.4 | Edit a ticket | U1 | u1-foundation-tickets | 4 |
| US1.5 | Change a ticket's status | U1 | u1-foundation-tickets | 5 |
| US4.1 | Get clear, safe error messages | U1 | u1-foundation-tickets | 6 |
| US2.5 | Choose the mock or the real Groq AI | U2 | u2-ai-assistant | 1 |
| US2.2 | Reject bad AI output | U2 | u2-ai-assistant | 2 |
| US2.1 | Analyze a ticket with AI | U2 | u2-ai-assistant | 3 |
| US2.4 | Get a reply draft to review | U2 | u2-ai-assistant | 4 |
| US2.3 | Keep working when the AI is down | U2 | u2-ai-assistant | 5 |
| US3.1 | See the queue at a glance | U3 | u3-dashboard | 1 |
| US4.4 | Keep secrets out of the repository | U4 | u4-release-kit | 1 |
| US4.3 | Try every endpoint with example requests | U4 | u4-release-kit | 2 |
| US4.2 | Run the project from a clean clone (release gate) | U4 | u4-release-kit | 3 |

Order within U1 follows the thin slice: create and get first (persistence across restart), then list, update, status, and full error handling. Order within U2 builds the mock and validator before the flows that use them.

## Cross-Cutting Rows

| Concern | Primary unit | Also touches | Note |
|---|---|---|---|
| Error responses (US4.1) | U1 | U2 | U1 builds the global handler; U2 adds typed AI failures it maps. |
| AI unavailable (US2.3, AC2.3.4) | U2 | U1, U3 | CRUD and dashboard must not call the AI; verified by U1/U3 tests running with no AI. |
| Secrets out of Git (US4.4) | U4 | U1, U2 | U1 adds `.gitignore`; U2 reads the key from user secrets/env only; U4 verifies. |
| Shared enums | U1 | U2, U3 | One shared Domain folder (project rule). |

## Coverage Verification

- All 15 stories (US1.1–US1.5, US2.1–US2.5, US3.1, US4.1–US4.4) are assigned exactly one primary unit.
- Every unit has rows: U1 (6), U2 (5), U3 (1), U4 (3).
