using SupportDesk.Api.Domain;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Tests.TestSupport;

namespace SupportDesk.Api.Tests.Tickets;

/// <summary>
/// The analyze and draft flows (BR7.1-BR7.6, BR9.x, FR-06, FR-07): validated results only, no partial writes,
/// 404 and rate limiting before the AI is called, drafts returned but never stored.
/// </summary>
[Trait("Unit", "U2")]
public sealed class TicketServiceAiTests : IDisposable
{
    private readonly ServiceHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public async Task Analyze_SavesTheAnalysis_AndOverwritesPriority()
    {
        var seeded = await _harness.SeedAsync("Cannot sign in", TicketStatus.Closed, TicketPriority.Low);
        _harness.Clock.Advance(TimeSpan.FromMinutes(3));

        var outcome = await _harness.Tickets().AnalyzeAsync(seeded.Id, CancellationToken.None);

        Assert.Equal(TicketPriority.Low, outcome.PreviousPriority);
        var saved = await _harness.ReloadAsync(seeded.Id);
        var expected = FakeAiTicketService.DefaultAnalysis;
        Assert.Equal(expected.Category, saved!.Category);
        Assert.Equal(expected.Sentiment, saved.Sentiment);
        Assert.Equal(expected.Summary, saved.AiSummary);
        Assert.Equal(expected.SuggestedAction, saved.SuggestedAction);
        Assert.Equal(expected.Priority, saved.Priority);
        Assert.Equal(TicketStatus.Closed, saved.Status);
        Assert.Equal(_harness.Clock.UtcNow, saved.UpdatedAt);
    }

    [Fact]
    public async Task Analyze_Again_ReplacesTheEarlierAnalysis()
    {
        var seeded = await _harness.SeedAsync("Billing question");
        await _harness.Tickets().AnalyzeAsync(seeded.Id, CancellationToken.None);

        _harness.Ai.Analysis = FakeAiTicketService.DefaultAnalysis with
        {
            Category = TicketCategory.Billing,
            Summary = "A billing question.",
        };
        await _harness.Tickets().AnalyzeAsync(seeded.Id, CancellationToken.None);

        var saved = await _harness.ReloadAsync(seeded.Id);
        Assert.Equal(TicketCategory.Billing, saved!.Category);
        Assert.Equal("A billing question.", saved.AiSummary);
    }

    public static TheoryData<Exception> AiFailures() => new()
    {
        new AiInvalidOutputException("invalid"),
        new AiProviderException("provider"),
        new AiTimeoutException("timeout"),
        new AiNotConfiguredException("no key"),
    };

    [Theory]
    [MemberData(nameof(AiFailures))]
    public async Task Analyze_WhenTheAiFails_LeavesTheTicketUnchanged(Exception failure)
    {
        var seeded = await _harness.SeedAsync("Unchanged", priority: TicketPriority.Low);
        _harness.Ai.Failure = failure;

        var thrown = await Assert.ThrowsAnyAsync<Exception>(
            () => _harness.Tickets().AnalyzeAsync(seeded.Id, CancellationToken.None));

        Assert.Same(failure, thrown);
        var saved = await _harness.ReloadAsync(seeded.Id);
        Assert.Null(saved!.Category);
        Assert.Equal(TicketPriority.Low, saved.Priority);
        Assert.Equal(seeded.UpdatedAt, saved.UpdatedAt);
    }

    [Fact]
    public async Task Analyze_UnknownId_IsNotFound_AndNeverCallsTheAi()
    {
        await Assert.ThrowsAsync<TicketNotFoundException>(
            () => _harness.Tickets().AnalyzeAsync(999, CancellationToken.None));

        Assert.Equal(0, _harness.Ai.AnalyzeCalls);
        Assert.Equal(0, _harness.Limiter.Calls);
    }

    [Fact]
    public async Task Analyze_OverTheBudget_IsRateLimited_AndNeverCallsTheAi()
    {
        var seeded = await _harness.SeedAsync("Budget");
        _harness.Limiter.Allow = false;

        var failure = await Assert.ThrowsAsync<AiRateLimitedException>(
            () => _harness.Tickets().AnalyzeAsync(seeded.Id, CancellationToken.None));

        Assert.Equal(30, failure.RetryAfterSeconds);
        Assert.Equal(0, _harness.Ai.AnalyzeCalls);
        Assert.Null((await _harness.ReloadAsync(seeded.Id))!.Category);
    }

    [Fact]
    public async Task Draft_ReturnsTheDraft_WithoutChangingTheTicket()
    {
        var seeded = await _harness.SeedAsync("Draft me");
        _harness.Clock.Advance(TimeSpan.FromMinutes(7));

        var draft = await _harness.Tickets().DraftReplyAsync(seeded.Id, CancellationToken.None);

        Assert.Equal(seeded.Id, draft.TicketId);
        Assert.Equal(FakeAiTicketService.DefaultDraft, draft.Draft);
        Assert.Equal(_harness.Clock.UtcNow, draft.GeneratedAt);
        Assert.Equal(seeded.UpdatedAt, (await _harness.ReloadAsync(seeded.Id))!.UpdatedAt);
    }

    [Fact]
    public async Task Draft_PassesTheSavedAnalysis_OnlyWhenTheTicketWasAnalyzed()
    {
        var fresh = await _harness.SeedAsync("Not analyzed");
        await _harness.Tickets().DraftReplyAsync(fresh.Id, CancellationToken.None);
        Assert.Null(_harness.Ai.LastDraftContext);

        await _harness.Tickets().AnalyzeAsync(fresh.Id, CancellationToken.None);
        await _harness.Tickets().DraftReplyAsync(fresh.Id, CancellationToken.None);

        var context = _harness.Ai.LastDraftContext;
        Assert.NotNull(context);
        Assert.Equal(FakeAiTicketService.DefaultAnalysis.Category, context!.Category);
        Assert.Equal(FakeAiTicketService.DefaultAnalysis.Summary, context.Summary);
    }

    [Fact]
    public async Task Draft_UnknownId_OrOverBudget_NeverCallsTheAi()
    {
        var seeded = await _harness.SeedAsync("Budget");

        await Assert.ThrowsAsync<TicketNotFoundException>(
            () => _harness.Tickets().DraftReplyAsync(999, CancellationToken.None));
        _harness.Limiter.Allow = false;
        await Assert.ThrowsAsync<AiRateLimitedException>(
            () => _harness.Tickets().DraftReplyAsync(seeded.Id, CancellationToken.None));

        Assert.Equal(0, _harness.Ai.DraftCalls);
    }
}
