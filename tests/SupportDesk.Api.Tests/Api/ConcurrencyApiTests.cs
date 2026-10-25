using System.Net;
using System.Net.Http.Json;

namespace SupportDesk.Api.Tests.Api;

/// <summary>
/// The API under parallel requests on a real SQLite file, where every request opens its own connection exactly as
/// in production: one DbContext per request and SQLite's write locking keep every write intact.
/// </summary>
[Trait("Unit", "U3")]
public sealed class ConcurrencyApiTests : ApiTestBase
{
    public ConcurrencyApiTests()
        : base(useFileDatabase: true)
    {
    }

    [Fact]
    public async Task ParallelCreates_AllSucceed_AndTheCountsAddUp()
    {
        var responses = await Task.WhenAll(Enumerable.Range(1, 20).Select(i => Client.PostAsJsonAsync(
            "/api/tickets",
            new { title = $"Parallel ticket {i}", description = "Created at the same time as the others." })));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        var list = await JsonOf(await Client.GetAsync("/api/tickets?pageSize=50"));
        var summary = await JsonOf(await Client.GetAsync("/api/dashboard/summary"));
        Assert.Equal(20, list.GetProperty("totalCount").GetInt32());
        Assert.Equal(20, summary.GetProperty("total").GetInt32());
        Assert.Equal(20, summary.GetProperty("open").GetInt32());

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }
}
