using SupportDesk.Api.Features.Dashboard.Models;

namespace SupportDesk.Api.Features.Dashboard;

/// <summary>
/// Dashboard summary counts (FR9, ADR-003). Read-only: it never writes tickets and never calls the AI, so it
/// keeps working when the AI provider is unavailable (NFR1). Shared by the REST endpoint and, later, the
/// <c>get_dashboard_summary</c> MCP tool (contract C6).
/// </summary>
public interface IDashboardService
{
    /// <summary>Counts tickets by status, high priority and analysis state.</summary>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>The counts; all zero when there are no tickets.</returns>
    Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken);
}
