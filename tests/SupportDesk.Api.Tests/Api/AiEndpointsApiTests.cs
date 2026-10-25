using System.Net;
using SupportDesk.Api.Errors;

namespace SupportDesk.Api.Tests.Api;

/// <summary>
/// The AI endpoints end to end with the AI fake (FR-06, FR-07, FR-09): validated results, controlled errors with no
/// stack traces, drafts returned but not stored, and the AI budget enforced.
/// </summary>
[Trait("Unit", "U2")]
public sealed class AiEndpointsApiTests : ApiTestBase
{
    [Fact]
    public async Task Analyze_Returns200_WithTheAnalysis_AndSavesIt()
    {
        var id = await CreateTicketAsync();

        using var response = await Client.PostAsync($"/api/tickets/{id}/analyze", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await JsonOf(response);
        Assert.Equal("AccountAccess", json.GetProperty("category").GetString());
        Assert.Equal("Medium", json.GetProperty("previousPriority").GetString());
        Assert.Equal("High", json.GetProperty("ticket").GetProperty("priority").GetString());

        var stored = await JsonOf(await Client.GetAsync($"/api/tickets/{id}"));
        Assert.Equal("Frustrated", stored.GetProperty("sentiment").GetString());
    }

    [Fact]
    public async Task DraftReply_Returns200_WithTheNotice_AndDoesNotChangeTheTicket()
    {
        var id = await CreateTicketAsync();
        var before = await (await Client.GetAsync($"/api/tickets/{id}")).Content.ReadAsStringAsync();

        using var response = await Client.PostAsync($"/api/tickets/{id}/draft-reply", null);

        var json = await JsonOf(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(id, json.GetProperty("ticketId").GetInt32());
        Assert.Equal("Draft — requires human review; not sent", json.GetProperty("notice").GetString());
        var after = await (await Client.GetAsync($"/api/tickets/{id}")).Content.ReadAsStringAsync();
        Assert.Equal(before, after);
    }

    public static TheoryData<Exception, HttpStatusCode> Failures() => new()
    {
        { new AiInvalidOutputException("internal: bad json"), HttpStatusCode.BadGateway },
        { new AiProviderException("internal: 500 from provider"), HttpStatusCode.BadGateway },
        { new AiNotConfiguredException("internal: no key"), HttpStatusCode.ServiceUnavailable },
        { new AiTimeoutException("internal: slow"), HttpStatusCode.GatewayTimeout },
        { new InvalidOperationException("internal: boom"), HttpStatusCode.InternalServerError },
    };

    [Theory]
    [MemberData(nameof(Failures))]
    public async Task AiFailure_IsAControlledError_WithoutInternals_AndNothingSaved(Exception failure, HttpStatusCode status)
    {
        var id = await CreateTicketAsync();
        Factory.Ai.Failure = failure;

        using var response = await Client.PostAsync($"/api/tickets/{id}/analyze", null);

        Assert.Equal(status, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("internal:", body);
        Assert.DoesNotContain("   at ", body);
        Assert.Contains("traceId", body);
        var stored = await JsonOf(await Client.GetAsync($"/api/tickets/{id}"));
        Assert.Equal("Medium", stored.GetProperty("priority").GetString());
    }

    [Fact]
    public async Task OverTheBudget_Returns429_WithRetryAfter()
    {
        var id = await CreateTicketAsync();
        Factory.Limiter.Allow = false;

        using var response = await Client.PostAsync($"/api/tickets/{id}/draft-reply", null);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("30", response.Headers.GetValues("Retry-After").Single());
        Assert.Equal(0, Factory.Ai.DraftCalls);
    }

    [Fact]
    public async Task UnknownTicket_Is404_AndTheAiIsNotCalled()
    {
        using var response = await Client.PostAsync("/api/tickets/999/analyze", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, Factory.Ai.AnalyzeCalls);
    }

    [Fact]
    public async Task TicketOperations_KeepWorking_WhenTheAiIsDown()
    {
        Factory.Ai.Failure = new AiProviderException("down");

        var id = await CreateTicketAsync();
        using var list = await Client.GetAsync("/api/tickets");
        using var dashboard = await Client.GetAsync("/api/dashboard/summary");

        Assert.True(id > 0);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
    }
}
