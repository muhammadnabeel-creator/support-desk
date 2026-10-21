# Functional Design — U2 Questions

Requirements (FR7, FR8, FR11), stories (US2.1–US2.5) and contracts (C1–C4) already settle validation rules, error codes, priority overwrite and draft storage. Two choices remain.

### Q1. How should the mock AI decide its answers?
The mock is the default provider (no key needed) and must be clearly marked `[MOCK]`.

A. (Suggested) Simple keyword rules on the ticket text, so the demo looks sensible and is repeatable: e.g. "password/login/locked" → AccountAccess, "invoice/charge/refund" → Billing, "error/crash/bug" → TechnicalIssue; "urgent/cannot/down" → High priority; "angry/frustrated/unacceptable" → Frustrated sentiment; otherwise GeneralInquiry, Medium, Neutral
B. Always return the same fixed answer for every ticket
X. Other (please specify)

[Answer]: X. Other: "lets go with groq no mock ai" — confirmed in FQ1: Groq only, no mock

### Q2. Which Groq model should be the default?
The model is configuration (`Ai:Groq:Model`) and can be changed without code; this only sets the README/appsettings default.

A. (Suggested) A capable general model (e.g. `llama-3.3-70b-versatile`) — better JSON and summaries; still fast on Groq
B. A small fast model (e.g. `llama-3.1-8b-instant`) — quicker and cheaper, slightly weaker output
X. Other (please specify)

[Answer]: A

### FQ1. Confirm Groq only, no mock provider?
Consequence: the reviewer needs their own Groq key for the two AI endpoints (503 without it); requirements FR11, US2.5 and US4.2 are updated.

A. Groq only, no mock
B. Keep a mock only as a no-key fallback for the reviewer
X. Other (please specify)

[Answer]: A
