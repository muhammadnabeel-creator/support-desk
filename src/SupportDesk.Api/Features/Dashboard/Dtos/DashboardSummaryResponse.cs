using SupportDesk.Api.Features.Dashboard.Models;

namespace SupportDesk.Api.Features.Dashboard.Dtos;

/// <summary>Body of <c>GET /api/dashboard/summary</c> (contract C1 <c>DashboardSummary</c>).</summary>
public sealed record DashboardSummaryResponse
{
    /// <summary>All tickets.</summary>
    /// <example>4</example>
    public required int Total { get; init; }

    /// <summary>Tickets with status Open.</summary>
    /// <example>3</example>
    public required int Open { get; init; }

    /// <summary>Tickets with status InProgress.</summary>
    /// <example>0</example>
    public required int InProgress { get; init; }

    /// <summary>Tickets with status Resolved.</summary>
    /// <example>1</example>
    public required int Resolved { get; init; }

    /// <summary>Tickets with status Closed.</summary>
    /// <example>0</example>
    public required int Closed { get; init; }

    /// <summary>Tickets with priority High or Critical, in any status.</summary>
    /// <example>2</example>
    public required int HighPriority { get; init; }

    /// <summary>Tickets not analyzed yet (no category set).</summary>
    /// <example>1</example>
    public required int Unanalyzed { get; init; }

    /// <summary>Maps the service result to its API representation.</summary>
    /// <param name="summary">The counts from <see cref="IDashboardService"/>.</param>
    /// <returns>The response body.</returns>
    public static DashboardSummaryResponse FromSummary(DashboardSummary summary) => new()
    {
        Total = summary.Total,
        Open = summary.Open,
        InProgress = summary.InProgress,
        Resolved = summary.Resolved,
        Closed = summary.Closed,
        HighPriority = summary.HighPriority,
        Unanalyzed = summary.Unanalyzed,
    };
}
