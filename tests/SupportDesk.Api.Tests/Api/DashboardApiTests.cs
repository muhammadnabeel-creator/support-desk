using System.Net.Http.Json;

namespace SupportDesk.Api.Tests.Api;

/// <summary>The dashboard endpoint end to end (FR-08, US3.1).</summary>
[Trait("Unit", "U3")]
public sealed class DashboardApiTests : ApiTestBase
{
    [Fact]
    public async Task Summary_WithNoTickets_IsAllZeros()
    {
        var json = await JsonOf(await Client.GetAsync("/api/dashboard/summary"));

        foreach (var field in new[] { "total", "open", "inProgress", "resolved", "closed", "highPriority", "unanalyzed" })
        {
            Assert.Equal(0, json.GetProperty(field).GetInt32());
        }
    }

    [Fact]
    public async Task Summary_FollowsStatusAndPriorityChanges()
    {
        var first = await CreateTicketAsync("First");
        await CreateTicketAsync("Second");
        await Client.PatchAsJsonAsync($"/api/tickets/{first}/status", new { status = "Resolved" });
        await Client.PutAsJsonAsync(
            $"/api/tickets/{first}",
            new { title = "First", description = "I reset my password but still cannot sign in.", priority = "Critical" });

        var json = await JsonOf(await Client.GetAsync("/api/dashboard/summary"));

        Assert.Equal(2, json.GetProperty("total").GetInt32());
        Assert.Equal(1, json.GetProperty("open").GetInt32());
        Assert.Equal(1, json.GetProperty("resolved").GetInt32());
        Assert.Equal(1, json.GetProperty("highPriority").GetInt32());
        Assert.Equal(2, json.GetProperty("unanalyzed").GetInt32());
    }
}
