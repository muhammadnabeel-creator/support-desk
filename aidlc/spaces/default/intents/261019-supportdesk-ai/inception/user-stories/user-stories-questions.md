# User Stories — Plan and Questions

## Story Plan (draft)

- **Source**: `inception/requirements-analysis/requirements.md` (FR1–FR12, NFR1–NFR10), with practices from `inception/practices-discovery/team-practices.md`.
- **Format**: "As a [persona], I want [goal], so that [benefit]"; INVEST; 3–6 Given/When/Then acceptance criteria each; IDs `US{group}.{seq}` and `AC{group}.{seq}.{n}`.
- **Prioritization**: MoSCoW per story; the MVP line is decided later in Delivery Planning.

## Questions

### Q1. Who are the personas?
The brief names "users" and "agents". Stories need a defined person for each goal.

A. (Suggested) Three: **Sam, Support Agent** (primary — works tickets and uses AI help), **Lee, Support Lead** (reads the dashboard to see workload), **Riley, Reviewer/Trainer** (clones the repo, runs it from the README, checks safety rules)
B. Two: Support Agent and Support Lead only (the reviewer is not a product user)
C. One: Support Agent does everything, including the dashboard
X. Other (please specify)

[Answer]: A

### Q2. How should stories be grouped?
A. (Suggested) By feature area, matching the requirement groups: Ticket Management, AI Assistance, Dashboard, Reliability & Setup
B. By persona
C. By the brief's 5-day plan (Day 1 stories, Day 2 stories, ...)
X. Other (please specify)

[Answer]: A

### Q3. How fine-grained should stories be?
A. (Suggested) One story per endpoint/behaviour (about 12–15 stories), e.g. separate stories for "create", "list with filters", "analyze", "handle bad AI output"
B. Coarser: one story per feature area (about 5–6 stories, each with more criteria)
C. Finer: separate stories for every error case (about 25+ stories)
X. Other (please specify)

[Answer]: A

### Q4. Should the brief's submission items be stories?
The brief also asks for example requests/responses, a demo and a code walkthrough.

A. (Suggested) Yes, as Should Have stories for the Reviewer persona: run from a clean clone with the mock, and example requests (a `.http` file) for every endpoint; the live demo and walkthrough stay out of the stories (they are events, not software)
B. No, keep stories to API behaviour only
C. Yes, include the demo and walkthrough as stories too
X. Other (please specify)

[Answer]: A
