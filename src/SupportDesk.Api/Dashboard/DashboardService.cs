using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Dashboard;

/// <summary>
/// Computes the dashboard counts with read-only queries over the Ticket table (ADR-003). Each count is one
/// <c>COUNT</c> query in SQLite, so no tickets are loaded into memory.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly SupportDeskDbContext _db;

    /// <summary>Creates the service.</summary>
    /// <param name="db">The database context; used for reads only.</param>
    public DashboardService(SupportDeskDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var tickets = _db.Tickets.AsNoTracking();

        var total = await tickets.CountAsync(cancellationToken);
        var open = await tickets.CountAsync(t => t.Status == TicketStatus.Open, cancellationToken);
        var inProgress = await tickets.CountAsync(t => t.Status == TicketStatus.InProgress, cancellationToken);
        var resolved = await tickets.CountAsync(t => t.Status == TicketStatus.Resolved, cancellationToken);
        var closed = await tickets.CountAsync(t => t.Status == TicketStatus.Closed, cancellationToken);
        var highPriority = await tickets.CountAsync(
            t => t.Priority == TicketPriority.High || t.Priority == TicketPriority.Critical,
            cancellationToken);
        var unanalyzed = await tickets.CountAsync(t => t.Category == null, cancellationToken);

        return new DashboardSummary(total, open, inProgress, resolved, closed, highPriority, unanalyzed);
    }
}
