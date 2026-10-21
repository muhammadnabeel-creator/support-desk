# External Dependency Map — SupportDesk AI

A **Bolt** is one build pass over a piece of the work that ends in something that runs and is tested (`bolt-plan.md`).

## Dependencies

| Dependency | Owner | Needed by | Lead time | Status (Q4) | If it slips |
|---|---|---|---|---|---|
| Groq API key | You (from console.groq.com) | Bolt 2 manual Groq check | Minutes | Available before U2 | Build and test against the mock; demo with the mock and show the Groq adapter code |
| Groq service availability | Groq | Bolt 2 manual check; live demo | — | External | Same fallback as above |
| .NET 8 SDK | You | Bolt 1 | Installed (8.0.131) | Ready | — |
| `dotnet-ef` CLI tool | NuGet | Bolt 1 migrations | Minutes (`dotnet tool install`) | To install in Bolt 1 | Document in README |
| NuGet packages (EF Core SQLite, Swashbuckle, xUnit, coverlet, Mvc.Testing) | NuGet | Bolt 1 | Minutes | Online restore | — |
| `ModelContextProtocol` NuGet package (official C# MCP SDK) | NuGet / MCP project | Bolt 4 (U5) | Minutes | To add in U5 | Pin the version that restores; the SDK is pre-1.0 in places, so follow its current README |
| Claude Code as MCP client (MQ2) | You | Bolt 4 manual check; evidence for U4; live demo | Ready (installed) | Ready | Use MCP Inspector (`npx @modelcontextprotocol/inspector`) for the evidence instead |
| GitHub repository and reviewer access | You / trainer | Every push; Bolt 4 submission | Minutes | To create | Push before Day 5 at the latest |

No other team, approval or data source gates any Bolt.
