# Security Requirements — U2 AI Assistant

Sources: NFR2, NFR4, NFR5 and FR11 in `inception/requirements-analysis/requirements.md`; hard rules in `aidlc/spaces/default/memory/project.md`; rules BR7.3, BR8.x, BR9.4, BR10.x, BR11.x in `construction/u2-ai-assistant/functional-design/rules.md` and `functional-spec.md`; contract C4 in `inception/contract-design/contract-summary.md`.

| ID | Requirement | Verification |
|---|---|---|
| NFR2.13 | The Groq API key is read only from configuration sources outside the repo (`dotnet user-secrets` in Development, environment variable `Ai__Groq__ApiKey`); committed `appsettings.json` holds an empty placeholder | Inspect committed files; U4 secrets check |
| NFR2.14 | The key is sent only in the `Authorization: Bearer` header to the configured Groq base URL over HTTPS; it is never logged, put in exception messages, or returned | Code review; grep logs during manual test |
| NFR2.15 | Typed AI exceptions carry fixed generic messages only (no provider body, no ticket text, no key); inner exceptions are network/timeout exceptions only, never response bodies | Code review of `Errors/Ai*Exception` usage |
| NFR2.16 | Logs for AI calls contain only operation (analyze/draft), model, HTTP status, duration and outcome — never ticket text, prompts or AI output | Code review; manual log check |
| NFR4.1 | AI output is untrusted: parsed with System.Text.Json into known fields only, validated (BR8.1–BR8.3, BR9.1) before any use; never executed, never used in SQL, never maps to Status | Code review; Task F tests with bad outputs |
| NFR4.2 | Prompt injection is mitigated, not assumed impossible: ticket text is delimited as data in the user message; instructions and the allowed-values schema live in the system message; the validator and the fixed field mapping (BR8.5) are the real safeguard | Code review |
| NFR4.3 | The Groq base URL is configuration with an HTTPS default; a non-HTTPS or empty base URL is rejected at startup with a clear message | Startup check |
| NFR5.1 | Only title and description (and the saved analysis for drafts) leave the app; ticket id, timestamps and draft history are never sent (BR7.3) | Code review |
| NFR5.2 | README states only demo/synthetic data may be used with Groq (BR11.2, delivered in U4) | U4 review |
| NFR2.17 | No reply-sending capability exists (no SMTP, webhook or messaging client) (BR9.4) | Code review |

Threat notes: no authentication (out of scope, local use); a malicious ticket could try to steer the model — the worst outcome is a valid-but-wrong analysis or draft, which a human reviews; it can never change status, run code, or send anything.
