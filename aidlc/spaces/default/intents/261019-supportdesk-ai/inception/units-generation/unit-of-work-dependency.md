# Unit Dependencies — SupportDesk AI

Topology only. The build order is chosen in Delivery Planning. Units are defined in `unit-of-work.md`; components and decisions come from `inception/domain-design/components.md` and `decisions.md`.

## Dependency Graph

```mermaid
graph LR
    U2["U2 AI Assistant"] --> U1["U1 Foundation & Tickets"]
    U3["U3 Dashboard"] --> U1
    U4["U4 Release Kit"] --> U1
    U4 --> U2
    U4 --> U3
    U5["U5 MCP Server"] --> U1
    U5 --> U2
    U5 --> U3
    U4 --> U5
```

<!-- Text fallback: U2 depends on U1. U3 depends on U1. U5 depends on U1, U2 and U3. U4 depends on U1, U2, U3 and U5. U1 depends on nothing. There are no cycles. -->

| Unit | Depends on | Why |
|---|---|---|
| U1 Foundation & Tickets | — | The base project, database and Ticket entity everything else uses. |
| U2 AI Assistant | U1 | Its flows load and save tickets via the Tickets service and DbContext; its failures are mapped by U1's exception handler. |
| U3 Dashboard | U1 | Counts the Ticket table created by U1. |
| U5 MCP Server | U1, U2, U3 | Its tools call the Tickets service (U1), the analyze and draft flows (U2) and the Dashboard service (U3); it reuses U1's registration, database and error titles. |
| U4 Release Kit | U1, U2, U3, U5 | Documents and example-requests every endpoint and the MCP setup, and stores the MCP evidence, so all of them must exist. |

## Integration Points

| From | To | Mechanism | What crosses |
|---|---|---|---|
| U2 | U1 | In-process service call + shared DbContext | Ticket load/save for analyze and draft; AI-field writes on Ticket |
| U2 | U1 | Exceptions to the global handler | Typed AI failures (invalid output, timeout, provider error, not configured) -> 502/504/502/503 |
| U2, U1 | shared Domain folder (in U1) | Shared enums | Status, Priority, Category, Sentiment |
| U3 | U1 | Read-only EF queries | Ticket table counts (schema coupling, no writes) |
| U5 | U1, U2 | In-process service calls (same methods as the controllers) | Ticket create/list/get/update/status, analyze and draft results, typed failures |
| U5 | U3 | In-process service call | Dashboard summary counts |
| U5 | U1 | Shared registration method and `Program.cs` | DbContext, clock, ticket service, AI options and client, dashboard service |
| U4 | U1, U2, U3, U5 | Documentation | Endpoint shapes, configuration keys, MCP client configuration and evidence |

## Parallel Development Opportunities

- After U1, **U2 and U3 have no dependency on each other** and can be built in either order or in parallel.
- U4 can start its README skeleton early but completes only after U1–U3 and U5.
- U1 and U2 are already built. The remaining valid order is U3, U5, U4 (U5 needs U3's dashboard service).

## Thin Slice

The classic scope runs no formal skeleton checkpoint (team practices: `skeleton: off`), but U1 is shaped so its first commits are the smallest working integrated slice: create, list and get a ticket through controller, service and SQLite, verified by `dotnet build && dotnet test` and by the ticket surviving a restart. U1 has no dependencies, so it can run before any later unit.

## Edge Block

```yaml
units:
  - name: u1-foundation-tickets
    kind: service
    depends_on: []
  - name: u2-ai-assistant
    kind: service
    depends_on: [u1-foundation-tickets]
  - name: u3-dashboard
    kind: service
    depends_on: [u1-foundation-tickets]
  - name: u4-release-kit
    kind: packaging
    depends_on: [u1-foundation-tickets, u2-ai-assistant, u3-dashboard, u5-mcp-server]
  - name: u5-mcp-server
    kind: service
    depends_on: [u1-foundation-tickets, u2-ai-assistant, u3-dashboard]
```
