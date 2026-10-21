# Team Allocation — SupportDesk AI

A **Bolt** is one build pass over a piece of the work that ends in something that runs and is tested (see `bolt-plan.md`). A **mob** is the group that builds a Bolt together.

## Allocation

Team Formation did not run (the classic workflow skips it), so there is one mob for every Bolt: **you (the trainee) as decision-maker and reviewer, with the AI developer (aidlc-developer-agent) writing the code** in this one session.

| Bolt | Unit | Mob | Your role |
|---|---|---|---|
| 1 Foundation & Tickets | U1 | You + AI developer | Approve the code plan; run and read the tests; commit and merge |
| 2 AI Assistant | U2 | You + AI developer | Same, plus supply the Groq key via user secrets and run the manual Groq check |
| 3 Dashboard | U3 | You + AI developer | Same |
| 4 MCP Server | U5 | You + AI developer | Same, plus connect Claude Code and save the evidence of the tool calls |
| 5 Release Kit | U4 | You + AI developer | Same, plus perform the clean-clone check yourself |
| — Task F tests | all | You + AI developer | Say when to start; review the tests and the coverage report |

## Working Mode

- One session, one Bolt at a time (Q3); no parallel work, so no cross-team coordination is needed.
- Because you must explain the code in the walkthrough, you approve every code plan before code is written and should read each unit's code and tests before merging (this covers the "walkthrough" worry in `risk-and-sequencing-rationale.md`).
