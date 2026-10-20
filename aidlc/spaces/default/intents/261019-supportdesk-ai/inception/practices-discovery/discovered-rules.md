# Discovered Rules — SupportDesk AI

These are the hard constraints the human confirmed in the practices-discovery
interview (Q8: A, B, C, D, E). Every later stage must follow them.

## Mandated

- ALWAYS keep core ticket operations working when the AI provider is unavailable, apply a timeout to every AI request, and never expose stack traces or secrets in error responses.

## Forbidden

- NEVER commit API keys, passwords or other secrets; configuration comes from environment variables or user secrets, and only placeholder examples are committed.
- NEVER send an AI-generated reply automatically; drafts are returned for human review only.
- NEVER save or return AI output that has not been validated against a typed DTO and the allowed values (status and priority enums).
- NEVER send real customer, confidential or personal data to an external AI provider.
- NEVER execute AI-generated code, SQL or commands.
