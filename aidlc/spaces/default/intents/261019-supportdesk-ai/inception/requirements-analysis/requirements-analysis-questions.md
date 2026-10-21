# Requirements Analysis — Questions

Source: the catalogued requirements brief (`documents/supportdesk-requirements.pdf`, FR-01 to FR-10) plus your affirmed practices. The brief is clear on endpoints; these questions cover the decisions it leaves open. Smaller corners (length limits, dashboard definitions, error format) are written as assumptions you can correct at the approval step.

## AI Integration

### Q1. Which AI provider will the real integration use?
The brief lets the trainer/team choose a hosted LLM or a local Ollama model, and allows a clearly labelled mock during development. This decides the configuration keys and the HTTP client we build.

A. (Suggested) Build against a clearly labelled mock first, then add a real provider behind the same `IAiTicketService`; the real one is a hosted LLM (OpenAI-compatible chat API), with the provider chosen by configuration
B. Mock first, then a real Anthropic Claude integration
C. Mock first, then a local Ollama model (no API key, runs on your machine)
D. Mock only for now; I'll confirm the real provider with my trainer later
X. Other (please specify)

[Answer]: X. Other: "we use grok i will prived api key  grok not xAI" (follow-up FQ1 confirms which provider)

### Q2. What should "analyze" do to the ticket's own Priority and Category?
The brief says to persist the analysis result after validation. The AI also suggests a priority, and the ticket already has a Priority field.

A. (Suggested) Save the validated suggestion into the ticket: Category, Sentiment, AiSummary and SuggestedAction are set, and Priority is overwritten with the AI's suggested priority
B. Save Category, Sentiment, AiSummary and SuggestedAction, but keep the ticket's Priority unchanged; the AI's priority is only returned in the response as a suggestion
C. Don't change the ticket at all; just return the analysis
X. Other (please specify)

[Answer]: A

### Q3. Which values may the AI choose for Category and Sentiment?
The brief says to validate AI output against allowed values. Priority (Low/Medium/High/Critical) is already fixed; Category and Sentiment are not.

A. (Suggested) Fixed lists. Category: Account Access, Billing, Technical Issue, Feature Request, General Inquiry, Other. Sentiment: Positive, Neutral, Negative, Frustrated. Anything else is rejected as malformed output
B. Fixed list for Sentiment only; Category is free text (length-limited)
C. Both free text (length-limited), no fixed lists
X. Other (please specify)

[Answer]: A

### Q4. Should generated reply drafts be stored?
The brief says return the draft for human review and never send it.

A. (Suggested) Return the draft only; nothing is stored, nothing is sent
B. Store the latest draft on the ticket so it can be retrieved later (still never sent)
X. Other (please specify)

[Answer]: B

## Tickets

### Q5. How should ticket status changes work?
The brief allows Open, InProgress, Resolved and Closed and says to reject invalid values; it does not say whether some moves are forbidden.

A. (Suggested) Any of the four statuses can be set at any time (simple; only unknown values are rejected)
B. Enforce a flow: Open -> InProgress -> Resolved -> Closed, with Resolved allowed back to InProgress (reopen), and Closed final
C. Enforce a flow but allow reopening a Closed ticket back to Open
X. Other (please specify)

[Answer]: A

### Q6. What can the person creating or editing a ticket set?
The brief's create example sends only title and description; PUT updates "permitted fields".

A. (Suggested) Create: title and description (Priority defaults to Medium, Status to Open). PUT: title, description and priority. Status changes only through the PATCH status endpoint; AI fields only through analyze
B. Create: title, description and optional priority. PUT: same as A
C. Create: title and description only. PUT: title and description only (priority comes only from AI analysis)
X. Other (please specify)

[Answer]: A

### Q7. What should the ticket ID be?
A. (Suggested) Integer, auto-incremented by the database (simple to type in Swagger)
B. GUID (not guessable, but harder to demo)
X. Other (please specify)

[Answer]: A

### Q8. How much should the ticket list support?
The brief says filters "if time allows".

A. (Suggested) Optional filters for status, priority and category, newest first, no pagination (small local dataset)
B. Filters plus simple pagination (page and pageSize, with a maximum page size)
C. No filters in the first version; return all tickets newest first
X. Other (please specify)

[Answer]: A

## Follow-up Questions

### FQ1. You said "grok, not xAI". Did you mean Groq?
Groq (groq.com, spelled with a q) is a hosted service that runs open models such as Llama very fast through an OpenAI-compatible API; you get a key from console.groq.com. Grok (with a k) is xAI's own model. Your note suggests Groq.

A. (Suggested) Yes, Groq (groq.com): OpenAI-compatible chat API at api.groq.com, model chosen by configuration, key supplied by me through user secrets / environment variable; a labelled mock is still built first and stays selectable for tests and demos without a key
B. Groq, and skip the mock (tests still use hand-written fakes)
C. No, I mean xAI's Grok after all
X. Other (please specify)

[Answer]: A

## Change Request 2026-10-21 — MCP server (brief FR-11 to FR-13)

The updated brief adds an MCP server so any MCP-compatible LLM client can use SupportDesk AI through 8 tools that call the existing services. FR-01 to FR-10 are unchanged.

### MQ1. Which MCP transports should we support?
The brief requires stdio; streamable HTTP is optional.

A. (Suggested) stdio only: the client starts the server as a local process; nothing new listens on the network
B. stdio plus streamable HTTP
X. Other (please specify)

[Answer]: A

### MQ2. Which MCP client do we use for the demo and the evidence (screenshot or log)?
You work on Linux, where Claude Desktop is not officially available.

A. (Suggested) MCP Inspector (official test client, runs with npx, no account needed) for the evidence, plus a sample config for Claude Desktop / Claude Code in the README
B. Claude Code as the client (uses your Claude account), plus MCP Inspector as backup
X. Other (please specify)

[Answer]: B

### MQ3. How should the MCP tools report errors (bad input, unknown id, AI timeout, provider error)?
A. (Suggested) A tool error result (`isError: true`) with the same short, safe title and detail as the REST ProblemDetails, plus the per-field validation messages; never stack traces or secrets
B. Throw protocol-level errors (JSON-RPC errors) instead of tool error results
X. Other (please specify)

[Answer]: A
