# MCP Evidence

Evidence that the SupportDesk MCP server works with an MCP client (brief FR-13 and the submission checklist).
All data shown is demo data.

| File | What it shows |
|---|---|
| `stdio-smoke-test.log` | A scripted MCP client talking to the server over stdio, started from another folder: the handshake, the eight tools listed, successful calls (including `analyze_ticket` and `draft_reply` through Groq), safe 400/404 error results, and a clean standard output |
| `claude-code-*.png` / `claude-code-session.md` | Claude Code connected to the server and calling `list_tickets`, `analyze_ticket` and `draft_reply` (added by the developer after the manual check) |

## How the Claude Code evidence was produced

1. `dotnet build` (from the repository root).
2. `claude mcp add supportdesk -- dotnet <repo>/src/SupportDesk.Api/bin/Debug/net8.0/SupportDesk.Api.dll --mcp`
3. Restart Claude Code; `/mcp` shows `supportdesk` connected with eight tools.
4. Prompts used: "list my open tickets", "create a ticket about a billing problem", "analyze ticket 1", "draft a reply for ticket 1", "show the dashboard summary".
5. Screenshots or the copied session were saved in this folder.
