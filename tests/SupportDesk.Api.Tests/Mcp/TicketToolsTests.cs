using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Mcp;
using SupportDesk.Api.Tests.TestSupport;

namespace SupportDesk.Api.Tests.Mcp;

/// <summary>
/// The eight MCP tools over the real services (FR14, FR15): same results as REST, strict input, safe errors,
/// no delete tool, and draft_reply never stores or sends anything.
/// </summary>
[Trait("Unit", "U5")]
public sealed class TicketToolsTests : IDisposable
{
    private readonly ServiceHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    private TicketTools Tools() => new(_harness.Tickets(), _harness.Dashboard(), NullLogger<TicketTools>.Instance);

    private static JsonElement Json(CallToolResult result) =>
        JsonDocument.Parse(Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text).RootElement;

    [Fact]
    public void ExactlyTheEightBriefTools_AreExposed_AndNoDelete()
    {
        var names = typeof(TicketTools)
            .GetMethods()
            .Select(method => method.GetCustomAttribute<McpServerToolAttribute>()?.Name)
            .OfType<string>()
            .Order()
            .ToList();

        Assert.Equal(
            [
                "analyze_ticket",
                "create_ticket",
                "draft_reply",
                "get_dashboard_summary",
                "get_ticket",
                "list_tickets",
                "update_ticket",
                "update_ticket_status",
            ],
            names);
        Assert.DoesNotContain(names, name => name.Contains("delete", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CreateThenGet_ReturnsTheSameTicketAsTheService()
    {
        var created = Json(await Tools().CreateTicketAsync("  Billing issue ", "I was billed twice this month.", CancellationToken.None));
        var id = created.GetProperty("id").GetInt32();

        var fetched = Json(await Tools().GetTicketAsync(id, CancellationToken.None));

        Assert.Equal("Billing issue", fetched.GetProperty("title").GetString());
        Assert.Equal("Open", fetched.GetProperty("status").GetString());
        Assert.Equal("Medium", fetched.GetProperty("priority").GetString());
    }

    [Fact]
    public async Task ListTickets_Pages_AndFilters()
    {
        for (var i = 0; i < 25; i++)
        {
            await _harness.SeedAsync($"Ticket {i}");
        }

        await _harness.SeedAsync("Closed one", Domain.TicketStatus.Closed);

        var firstPage = Json(await Tools().ListTicketsAsync(cancellationToken: CancellationToken.None));
        var closed = Json(await Tools().ListTicketsAsync(status: "closed", cancellationToken: CancellationToken.None));

        Assert.Equal(20, firstPage.GetProperty("pageSize").GetInt32());
        Assert.Equal(26, firstPage.GetProperty("totalCount").GetInt32());
        Assert.Equal(20, firstPage.GetProperty("items").GetArrayLength());
        Assert.Equal(1, closed.GetProperty("totalCount").GetInt32());
    }

    [Theory]
    [InlineData("Done")]
    [InlineData("1")]
    public async Task UpdateStatus_WithABadValue_IsA400_AndChangesNothing(string status)
    {
        var seeded = await _harness.SeedAsync("Status");

        var result = await Tools().UpdateTicketStatusAsync(seeded.Id, status, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(400, Json(result).GetProperty("status").GetInt32());
        Assert.Equal(Domain.TicketStatus.Open, (await _harness.ReloadAsync(seeded.Id))!.Status);
    }

    [Fact]
    public async Task UpdateTicket_And_UpdateStatus_Work()
    {
        var seeded = await _harness.SeedAsync("Before");

        Json(await Tools().UpdateTicketAsync(seeded.Id, "After", "A new long enough description.", "Critical", CancellationToken.None));
        var status = Json(await Tools().UpdateTicketStatusAsync(seeded.Id, "Resolved", CancellationToken.None));

        Assert.Equal("Resolved", status.GetProperty("status").GetString());
        Assert.Equal("Critical", status.GetProperty("priority").GetString());
        Assert.Equal("After", status.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData(0, 400)]
    [InlineData(-5, 400)]
    [InlineData(999, 404)]
    public async Task GetTicket_BadOrUnknownId_IsAnErrorResult(int id, int status)
    {
        var result = await Tools().GetTicketAsync(id, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(status, Json(result).GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task AnalyzeTicket_SavesAndReturnsThePreviousPriority()
    {
        var seeded = await _harness.SeedAsync("Analyze me", priority: Domain.TicketPriority.Low);

        var json = Json(await Tools().AnalyzeTicketAsync(seeded.Id, CancellationToken.None));

        Assert.Equal("Low", json.GetProperty("previousPriority").GetString());
        Assert.Equal("High", json.GetProperty("priority").GetString());
        Assert.Equal("AccountAccess", json.GetProperty("ticket").GetProperty("category").GetString());
    }

    [Fact]
    public async Task DraftReply_ReturnsTheNotice_AndStoresNothing()
    {
        var seeded = await _harness.SeedAsync("Draft me");

        var json = Json(await Tools().DraftReplyAsync(seeded.Id, CancellationToken.None));

        Assert.Equal(FakeAiTicketService.DefaultDraft, json.GetProperty("draft").GetString());
        Assert.Equal("Draft — requires human review; not sent", json.GetProperty("notice").GetString());
        Assert.Equal(seeded.UpdatedAt, (await _harness.ReloadAsync(seeded.Id))!.UpdatedAt);
    }

    [Fact]
    public async Task AiTimeout_IsA504ErrorResult_AndTheTicketIsUnchanged()
    {
        var seeded = await _harness.SeedAsync("Timeout");
        _harness.Ai.Failure = new AiTimeoutException("slow");

        var result = await Tools().AnalyzeTicketAsync(seeded.Id, CancellationToken.None);

        Assert.Equal(504, Json(result).GetProperty("status").GetInt32());
        Assert.Null((await _harness.ReloadAsync(seeded.Id))!.Category);
    }

    [Fact]
    public async Task DashboardSummary_MatchesTheData()
    {
        await _harness.SeedAsync("Open high", priority: Domain.TicketPriority.High);
        await _harness.SeedAsync("Closed", Domain.TicketStatus.Closed);

        var json = Json(await Tools().GetDashboardSummaryAsync(CancellationToken.None));

        Assert.Equal(2, json.GetProperty("total").GetInt32());
        Assert.Equal(1, json.GetProperty("highPriority").GetInt32());
        Assert.Equal(1, json.GetProperty("closed").GetInt32());
    }
}
