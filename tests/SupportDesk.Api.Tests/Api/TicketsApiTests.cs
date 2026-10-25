using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace SupportDesk.Api.Tests.Api;

/// <summary>The ticket endpoints end to end (FR-01 to FR-05, FR-09, FR3.3) through the real HTTP pipeline.</summary>
[Trait("Unit", "U1")]
public sealed class TicketsApiTests : ApiTestBase
{
    [Fact]
    public async Task Create_Returns201_WithLocation_AndTheTicket()
    {
        using var response = await Client.PostAsJsonAsync(
            "/api/tickets",
            new { title = "Unable to access my account", description = "I reset my password but still cannot sign in." });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await JsonOf(response);
        var id = json.GetProperty("id").GetInt32();
        Assert.EndsWith($"/api/tickets/{id}", response.Headers.Location!.ToString());
        Assert.Equal("Open", json.GetProperty("status").GetString());
        Assert.Equal("Medium", json.GetProperty("priority").GetString());
        Assert.EndsWith("Z", json.GetProperty("createdAt").GetString());
        Assert.False(json.TryGetProperty("draftReply", out _));

        using var fetched = await Client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
    }

    [Fact]
    public async Task Create_Invalid_Returns400ProblemDetails_NamingEveryField()
    {
        using var response = await Client.PostAsJsonAsync("/api/tickets", new { title = "  ", description = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var json = await JsonOf(response);
        Assert.True(json.GetProperty("errors").TryGetProperty("title", out _));
        Assert.True(json.GetProperty("errors").TryGetProperty("description", out _));
        Assert.True(json.TryGetProperty("traceId", out _));
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("""{"title": 5, "description": true}""")]
    public async Task MalformedBody_Returns400_Not500(string body)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await Client.PostAsync("/api/tickets", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("Exception", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_UnknownId_Is404_AndNonNumericId_Is400()
    {
        using var unknown = await Client.GetAsync("/api/tickets/999");
        using var notNumber = await Client.GetAsync("/api/tickets/abc");
        using var zero = await Client.GetAsync("/api/tickets/0");

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Contains("999", (await JsonOf(unknown)).GetProperty("detail").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, notNumber.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsPagedResults_NewestFirst_WithFilters()
    {
        for (var i = 1; i <= 3; i++)
        {
            await CreateTicketAsync($"Ticket {i}");
        }

        var page = await JsonOf(await Client.GetAsync("/api/tickets?page=1&pageSize=2"));
        var empty = await JsonOf(await Client.GetAsync("/api/tickets?status=closed"));

        Assert.Equal(3, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, page.GetProperty("totalPages").GetInt32());
        Assert.Equal("Ticket 3", page.GetProperty("items")[0].GetProperty("title").GetString());
        Assert.Equal(0, empty.GetProperty("items").GetArrayLength());
    }

    [Theory]
    [InlineData("/api/tickets?status=Pending")]
    [InlineData("/api/tickets?page=0")]
    [InlineData("/api/tickets?pageSize=201")]
    [InlineData("/api/tickets?page=abc")]
    public async Task List_InvalidQuery_Is400(string url)
    {
        using var response = await Client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesFields_ButNotStatus()
    {
        var id = await CreateTicketAsync();

        using var response = await Client.PutAsJsonAsync(
            $"/api/tickets/{id}",
            new { title = "Cannot sign in", description = "Still cannot sign in after two resets.", priority = "High", status = "Closed" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await JsonOf(response);
        Assert.Equal("High", json.GetProperty("priority").GetString());
        Assert.Equal("Open", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Update_MissingEverything_ListsAllFields()
    {
        var id = await CreateTicketAsync();

        using var response = await Client.PutAsJsonAsync($"/api/tickets/{id}", new { });

        var errors = (await JsonOf(response)).GetProperty("errors");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(errors.TryGetProperty("title", out _));
        Assert.True(errors.TryGetProperty("description", out _));
        Assert.True(errors.TryGetProperty("priority", out _));
    }

    [Fact]
    public async Task ChangeStatus_AcceptsTheFourStatuses()
    {
        var id = await CreateTicketAsync();

        foreach (var status in new[] { "InProgress", "Resolved", "Closed", "Open" })
        {
            using var response = await Client.PatchAsJsonAsync($"/api/tickets/{id}/status", new { status });
            Assert.Equal(status, (await JsonOf(response)).GetProperty("status").GetString());
        }
    }

    [Theory]
    [InlineData("""{"status":"Done"}""")]
    [InlineData("""{"status":1}""")]
    [InlineData("""{"status":"1"}""")]
    [InlineData("{}")]
    public async Task ChangeStatus_RejectsInvalidValues(string body)
    {
        var id = await CreateTicketAsync();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await Client.PatchAsync($"/api/tickets/{id}/status", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Allowed values", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnknownRoute_AndWrongMethod_ReturnProblemDetails()
    {
        using var missing = await Client.GetAsync("/api/nothing");
        using var wrongMethod = await Client.DeleteAsync("/api/tickets/1");

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, wrongMethod.StatusCode);
    }
}
