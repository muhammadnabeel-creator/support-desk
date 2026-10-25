using SupportDesk.Api.Domain;
using SupportDesk.Api.Features.Dashboard.Models;
using SupportDesk.Api.Tests.TestSupport;

namespace SupportDesk.Api.Tests.Dashboard;

/// <summary>Dashboard counts (FR9, AC9.1, US3.1): computed in one query, read-only, never calling the AI.</summary>
[Trait("Unit", "U3")]
public sealed class DashboardServiceTests : IDisposable
{
    private readonly ServiceHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public async Task NoTickets_GivesAllZeros()
    {
        var summary = await _harness.Dashboard().GetSummaryAsync(CancellationToken.None);

        Assert.Equal(new DashboardSummary(0, 0, 0, 0, 0, 0, 0), summary);
    }

    [Fact]
    public async Task BriefExample_ThreeOpenOneCritical_OneResolvedHigh()
    {
        await _harness.SeedAsync("Open 1", TicketStatus.Open, TicketPriority.Critical);
        await _harness.SeedAsync("Open 2", TicketStatus.Open);
        await _harness.SeedAsync("Open 3", TicketStatus.Open);
        await _harness.SeedAsync("Resolved", TicketStatus.Resolved, TicketPriority.High);

        var summary = await _harness.Dashboard().GetSummaryAsync(CancellationToken.None);

        Assert.Equal(4, summary.Total);
        Assert.Equal(3, summary.Open);
        Assert.Equal(1, summary.Resolved);
        Assert.Equal(2, summary.HighPriority);
    }

    [Fact]
    public async Task EveryCount_MatchesTheData()
    {
        await _harness.SeedAsync("Open", TicketStatus.Open, TicketPriority.Low, TicketCategory.Billing);
        await _harness.SeedAsync("In progress", TicketStatus.InProgress, TicketPriority.High);
        await _harness.SeedAsync("Resolved", TicketStatus.Resolved, TicketPriority.Medium, TicketCategory.Other);
        await _harness.SeedAsync("Closed 1", TicketStatus.Closed, TicketPriority.Critical);
        await _harness.SeedAsync("Closed 2", TicketStatus.Closed);

        var summary = await _harness.Dashboard().GetSummaryAsync(CancellationToken.None);

        Assert.Equal(new DashboardSummary(5, 1, 1, 1, 2, 2, 3), summary);
        Assert.Equal(summary.Total, summary.Open + summary.InProgress + summary.Resolved + summary.Closed);
    }

    [Fact]
    public async Task Summary_NeverCallsTheAi()
    {
        await _harness.SeedAsync("Any");

        await _harness.Dashboard().GetSummaryAsync(CancellationToken.None);

        Assert.Equal(0, _harness.Ai.AnalyzeCalls + _harness.Ai.DraftCalls);
    }
}
