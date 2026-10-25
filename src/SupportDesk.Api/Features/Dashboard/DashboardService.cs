using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Features.Dashboard.Models;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Features.Dashboard;

/// <summary>
/// Computes the dashboard counts with one read-only query over the Ticket table (ADR-003). All seven counts come
/// from the same <c>SELECT</c>, so they always describe one consistent moment even while other requests are
/// writing tickets, and no tickets are loaded into memory.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private static readonly DashboardSummary _empty = new(0, 0, 0, 0, 0, 0, 0);

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
        // Grouping every row under one constant key turns the seven conditional counts into a single SQL
        // statement (SELECT COUNT(*), COUNT(CASE WHEN ...), ...). With no tickets the query returns no row.
        var summary = await _db.Tickets
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new DashboardSummary(
                g.Count(),
                g.Count(t => t.Status == TicketStatus.Open),
                g.Count(t => t.Status == TicketStatus.InProgress),
                g.Count(t => t.Status == TicketStatus.Resolved),
                g.Count(t => t.Status == TicketStatus.Closed),
                g.Count(t => t.Priority == TicketPriority.High || t.Priority == TicketPriority.Critical),
                g.Count(t => t.Category == null)))
            .SingleOrDefaultAsync(cancellationToken);

        return summary ?? _empty;
    }
}
