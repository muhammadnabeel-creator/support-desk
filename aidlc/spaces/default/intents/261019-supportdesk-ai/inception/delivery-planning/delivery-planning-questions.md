# Delivery Planning — Questions

Inputs: units and their dependency graph (`inception/units-generation/unit-of-work.md`, `unit-of-work-dependency.md`, `unit-of-work-story-map.md`), contracts (`inception/contract-design/contract-summary.md`), components (`inception/domain-design/components.md`), requirements and stories, and team practices (thin slice first, one short-lived branch per unit merged with `--no-ff`, local build/test/format checks, 5-day window).

A **Bolt** is one build pass over a piece of the work that ends in something that runs and is tested (here: one or more units of work, built on one branch and merged). The dependency graph says U1 comes first, U2 and U3 can go in either order, and U4 comes last; this stage decides the order and what each Bolt must prove.

### Q1. What should we build first after the thin ticket slice (U1)?
A. (Suggested) Riskiest next: U1 -> U2 AI Assistant (Groq, validation, timeouts are the biggest unknowns and the most-graded part) -> U3 Dashboard -> U4 Release Kit
B. Easiest value next: U1 -> U3 Dashboard -> U2 AI Assistant -> U4 Release Kit
C. Score each unit with a formal model (value + urgency + risk reduction divided by size) and follow the scores
X. Other (please specify)

[Answer]: A

### Q2. How big should one Bolt be?
A. (Suggested) One unit per Bolt (4 Bolts), matching one branch per unit
B. Bundle U3 Dashboard with U4 Release Kit into one final Bolt (3 Bolts)
X. Other (please specify)

[Answer]: A

### Q3. Can Bolts be built at the same time?
A. (Suggested) No, one after another: one developer, one session, each Bolt merged before the next starts
B. Yes, U2 and U3 in parallel once U1 is merged
X. Other (please specify)

[Answer]: A

### Q4. When will your Groq API key be available?
It is the only thing outside this project that can hold us up; the mock covers development either way.

A. (Suggested) I already have it (or will before U2 starts); if it fails on the day, the demo falls back to the mock and the Groq call is shown in code
B. Not sure yet; build U2 against the mock and add Groq whenever the key arrives
X. Other (please specify)

[Answer]: A

### Q5. What worries you most about this build? (select all that apply)
A. Getting the Groq integration and AI-output validation right
B. EF Core migrations and SQLite setup
C. Finishing everything (including README and clean-clone check) within 5 days
D. Explaining the code confidently in the walkthrough
X. Other (please specify)

[Answer]: A, B, C, D
