using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Features.Dashboard;
using SupportDesk.Api.Features.Dashboard.Dtos;
using SupportDesk.Api.Features.Tickets;
using SupportDesk.Api.Features.Tickets.Dtos;
using SupportDesk.Api.Features.Tickets.Models;
using SupportDesk.Api.Features.Tickets.Validation;

namespace SupportDesk.Api.Mcp;

/// <summary>
/// The eight SupportDesk MCP tools (FR14, contract C5). Each tool maps its arguments to one call on the same
/// service method its REST endpoint uses and returns the same response shape, so validation, persistence and AI
/// safety come from the services unchanged (FR14.2, ADR-006). There is no delete tool and nothing is ever sent
/// to a customer (FR15.2, FR15.3).
/// </summary>
[McpServerToolType]
public sealed class TicketTools
{
    private const int McpDefaultPageSize = 20;
    private const int McpMaxPageSize = 50;

    private readonly ITicketService _tickets;
    private readonly IDashboardService _dashboard;
    private readonly ILogger<TicketTools> _logger;

    /// <summary>Creates the tools for one tool call (one DI scope, one database context).</summary>
    /// <param name="tickets">The ticket service shared with the REST controller.</param>
    /// <param name="dashboard">The dashboard service shared with the REST controller.</param>
    /// <param name="logger">Logger; writes to standard error in MCP mode.</param>
    public TicketTools(ITicketService tickets, IDashboardService dashboard, ILogger<TicketTools> logger)
    {
        _tickets = tickets;
        _dashboard = dashboard;
        _logger = logger;
    }

    /// <summary>Creates a ticket (same as <c>POST /api/tickets</c>).</summary>
    /// <param name="title">The title.</param>
    /// <param name="description">The description.</param>
    /// <param name="cancellationToken">Cancelled when the client cancels the call.</param>
    /// <returns>The created ticket, or an error result.</returns>
    [McpServerTool(Name = "create_ticket", Destructive = false)]
    [Description("Create a support ticket. New tickets are Open with Medium priority. Use demo data only; never real customer or personal data.")]
    public Task<CallToolResult> CreateTicketAsync(
        [Description("Ticket title, 3-200 characters after trimming.")] string title,
        [Description("Ticket description, 10-4000 characters after trimming.")] string description,
        CancellationToken cancellationToken) =>
        McpToolResults.RunAsync(
            async () => TicketResponse.FromTicket(
                await _tickets.CreateAsync(new CreateTicketInput(title, description), cancellationToken)),
            _logger,
            cancellationToken);

    /// <summary>Lists tickets (same as <c>GET /api/tickets</c>).</summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="priority">Optional priority filter.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="page">Optional 1-based page number (default 1).</param>
    /// <param name="pageSize">Optional page size, 1-50 for MCP (default 20).</param>
    /// <param name="cancellationToken">Cancelled when the client cancels the call.</param>
    /// <returns>One page of matching tickets, newest first, or an error result.</returns>
    [McpServerTool(Name = "list_tickets", ReadOnly = true)]
    [Description("List tickets, newest first, one page at a time. Optional filters are combined with AND. The result includes totalCount and totalPages; ask for the next page if you need more.")]
    public Task<CallToolResult> ListTicketsAsync(
        [Description("Optional status: Open, InProgress, Resolved or Closed.")] string? status = null,
        [Description("Optional priority: Low, Medium, High or Critical.")] string? priority = null,
        [Description("Optional category: AccountAccess, Billing, TechnicalIssue, FeatureRequest, GeneralInquiry or Other.")] string? category = null,
        [Description("Optional 1-based page number. Default 1.")] int? page = null,
        [Description("Optional tickets per page, 1-50. Default 20.")] int? pageSize = null,
        CancellationToken cancellationToken = default) =>
        McpToolResults.RunAsync(
            async () =>
            {
                // Smaller pages than REST keep the result small enough for an LLM's context.
                var paging = new PageRequest(page, pageSize ?? McpDefaultPageSize);
                var filter = TicketInputParsing.ParseListFilter(status, priority, category, paging, McpMaxPageSize);

                var tickets = await _tickets.ListAsync(filter, paging, cancellationToken);
                return PagedResponse<TicketResponse>.From(tickets, TicketResponse.FromTicket);
            },
            _logger,
            cancellationToken);

    /// <summary>Gets one ticket (same as <c>GET /api/tickets/{id}</c>).</summary>
    /// <param name="id">The ticket id.</param>
    /// <param name="cancellationToken">Cancelled when the client cancels the call.</param>
    /// <returns>The ticket, or an error result.</returns>
    [McpServerTool(Name = "get_ticket", ReadOnly = true)]
    [Description("Get one ticket by id, including its AI analysis and latest draft reply.")]
    public Task<CallToolResult> GetTicketAsync(
        [Description("Ticket id, a whole number of 1 or more.")] int id,
        CancellationToken cancellationToken) =>
        McpToolResults.RunAsync(
            async () => TicketResponse.FromTicket(
                await _tickets.GetAsync(TicketInputParsing.ValidateId(id), cancellationToken)),
            _logger,
            cancellationToken);

    /// <summary>Updates title, description and priority (same as <c>PUT /api/tickets/{id}</c>).</summary>
    /// <param name="id">The ticket id.</param>
    /// <param name="title">The new title.</param>
    /// <param name="description">The new description.</param>
    /// <param name="priority">The new priority.</param>
    /// <param name="cancellationToken">Cancelled when the client cancels the call.</param>
    /// <returns>The updated ticket, or an error result.</returns>
    [McpServerTool(Name = "update_ticket", Destructive = false, Idempotent = true)]
    [Description("Replace a ticket's title, description and priority. Status and AI fields cannot be changed here.")]
    public Task<CallToolResult> UpdateTicketAsync(
        [Description("Ticket id, a whole number of 1 or more.")] int id,
        [Description("New title, 3-200 characters after trimming.")] string title,
        [Description("New description, 10-4000 characters after trimming.")] string description,
        [Description("New priority: Low, Medium, High or Critical.")] string priority,
        CancellationToken cancellationToken) =>
        McpToolResults.RunAsync(
            async () =>
            {
                var ticketId = TicketInputParsing.ValidateId(id);
                var parsedPriority = TicketInputParsing.ParseEnumOrThrow<TicketPriority>(priority, nameof(priority));
                var ticket = await _tickets.UpdateAsync(
                    ticketId,
                    new UpdateTicketInput(title, description, parsedPriority),
                    cancellationToken);
                return TicketResponse.FromTicket(ticket);
            },
            _logger,
            cancellationToken);

    /// <summary>Sets a ticket's status (same as <c>PATCH /api/tickets/{id}/status</c>).</summary>
    /// <param name="id">The ticket id.</param>
    /// <param name="status">The new status.</param>
    /// <param name="cancellationToken">Cancelled when the client cancels the call.</param>
    /// <returns>The updated ticket, or an error result.</returns>
    [McpServerTool(Name = "update_ticket_status", Destructive = false, Idempotent = true)]
    [Description("Set a ticket's status. Any status may follow any status.")]
    public Task<CallToolResult> UpdateTicketStatusAsync(
        [Description("Ticket id, a whole number of 1 or more.")] int id,
        [Description("New status: Open, InProgress, Resolved or Closed.")] string status,
        CancellationToken cancellationToken) =>
        McpToolResults.RunAsync(
            async () =>
            {
                var ticketId = TicketInputParsing.ValidateId(id);
                var parsedStatus = TicketInputParsing.ParseEnumOrThrow<TicketStatus>(status, nameof(status));
                return TicketResponse.FromTicket(
                    await _tickets.ChangeStatusAsync(ticketId, parsedStatus, cancellationToken));
            },
            _logger,
            cancellationToken);

    /// <summary>Runs the AI analysis (same as <c>POST /api/tickets/{id}/analyze</c>).</summary>
    /// <param name="id">The ticket id.</param>
    /// <param name="cancellationToken">Cancelled when the client cancels the call.</param>
    /// <returns>The validated analysis with the previous priority and updated ticket, or an error result.</returns>
    [McpServerTool(Name = "analyze_ticket", Destructive = false, OpenWorld = true)]
    [Description("Ask the AI to suggest category, priority, sentiment, a summary and a next action. The validated result is saved and replaces the ticket's priority.")]
    public Task<CallToolResult> AnalyzeTicketAsync(
        [Description("Ticket id, a whole number of 1 or more.")] int id,
        CancellationToken cancellationToken) =>
        McpToolResults.RunAsync(
            async () => AnalysisResponse.FromOutcome(
                await _tickets.AnalyzeAsync(TicketInputParsing.ValidateId(id), cancellationToken)),
            _logger,
            cancellationToken);

    /// <summary>Drafts a reply for human review (same as <c>POST /api/tickets/{id}/draft-reply</c>).</summary>
    /// <param name="id">The ticket id.</param>
    /// <param name="cancellationToken">Cancelled when the client cancels the call.</param>
    /// <returns>The stored draft with its review notice, or an error result.</returns>
    [McpServerTool(Name = "draft_reply", Destructive = false, OpenWorld = true)]
    [Description("Ask the AI to draft a reply to the customer. The draft is returned for a human to review and edit; it is not saved and never sent.")]
    public Task<CallToolResult> DraftReplyAsync(
        [Description("Ticket id, a whole number of 1 or more.")] int id,
        CancellationToken cancellationToken) =>
        McpToolResults.RunAsync(
            async () => DraftReplyResponse.FromDraft(
                await _tickets.DraftReplyAsync(TicketInputParsing.ValidateId(id), cancellationToken)),
            _logger,
            cancellationToken);

    /// <summary>Returns the dashboard counts (same as <c>GET /api/dashboard/summary</c>).</summary>
    /// <param name="cancellationToken">Cancelled when the client cancels the call.</param>
    /// <returns>The counts, or an error result.</returns>
    [McpServerTool(Name = "get_dashboard_summary", ReadOnly = true)]
    [Description("Get ticket counts: total, open, inProgress, resolved, closed, highPriority (High or Critical) and unanalyzed.")]
    public Task<CallToolResult> GetDashboardSummaryAsync(CancellationToken cancellationToken) =>
        McpToolResults.RunAsync(
            async () => DashboardSummaryResponse.FromSummary(await _dashboard.GetSummaryAsync(cancellationToken)),
            _logger,
            cancellationToken);
}
