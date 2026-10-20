# Personas — SupportDesk AI

Source: `inception/requirements-analysis/requirements.md` (Intent Analysis, A1) and the Q1 answer in `user-stories-questions.md`. All three use the API through Swagger UI or `.http` requests; there is no frontend.

## Sam — Support Agent (primary)

- **Role**: handles incoming customer support tickets day to day.
- **Goals**:
  - Log, find, update and close tickets quickly.
  - Get a fast first read of a ticket (category, urgency, mood, summary, next step) without reading every word.
  - Start replies from a decent draft that Sam can edit before sending through the company's own channels.
- **Pain points**:
  - Triage takes time and priorities are set inconsistently.
  - An AI that silently sends or saves nonsense would be worse than no AI.
  - When the AI is down, Sam still has to work.
- **Tech comfort**: medium (comfortable with Swagger, not a developer).
- **Frequency**: all day, every working day.

## Lee — Support Lead

- **Role**: oversees the support queue and workload.
- **Goals**:
  - See at a glance how many tickets are open, in progress, resolved and closed.
  - Spot how many high-priority tickets need attention and how many haven't been analyzed yet.
- **Pain points**: no quick summary; counting by hand from a list is slow and error-prone.
- **Tech comfort**: medium.
- **Frequency**: a few times a day.

## Riley — Reviewer / Trainer

- **Role**: assesses the trainee's submission.
- **Goals**:
  - Clone the repository and run it by following the README, without needing an AI key.
  - Exercise every endpoint with ready-made example requests.
  - Confirm the safety rules hold: no secrets in Git, no stack traces in errors, AI output validated, drafts never sent.
- **Pain points**: projects that only run on the author's machine; hidden setup steps; secrets committed by accident.
- **Tech comfort**: high.
- **Frequency**: during review and the code walkthrough.

## Persona Relationships and Priority

| Rank | Persona | Why |
|---|---|---|
| 1 | Sam (Support Agent) | Uses every ticket and AI feature; most stories are Sam's. |
| 2 | Riley (Reviewer) | Decides whether the project passes; owns setup, safety and example-request stories. |
| 3 | Lee (Support Lead) | Reads the dashboard only; one story. |

Sam creates the data that Lee's dashboard summarises; Riley acts as Sam and Lee during the demo, then checks the setup and safety stories.
