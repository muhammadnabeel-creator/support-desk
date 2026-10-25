namespace SupportDesk.Api.Features.Dashboard.Models;

/// <summary>
/// Ticket counts for the support lead (FR9.1). Every count is zero when there are no tickets (AC9.1).
/// </summary>
/// <param name="Total">All tickets.</param>
/// <param name="Open">Tickets with status Open (InProgress is counted separately).</param>
/// <param name="InProgress">Tickets with status InProgress.</param>
/// <param name="Resolved">Tickets with status Resolved.</param>
/// <param name="Closed">Tickets with status Closed.</param>
/// <param name="HighPriority">Tickets with priority High or Critical, in any status.</param>
/// <param name="Unanalyzed">Tickets that have never been analyzed (no category set).</param>
public sealed record DashboardSummary(
    int Total,
    int Open,
    int InProgress,
    int Resolved,
    int Closed,
    int HighPriority,
    int Unanalyzed);
