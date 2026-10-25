using System.Net.Http.Json;
using System.Text.Json;
using SupportDesk.Api.Tests.TestSupport;

namespace SupportDesk.Api.Tests.Api;

/// <summary>A fresh in-memory API (own database, AI fake) per test, plus small helpers for JSON calls.</summary>
public abstract class ApiTestBase : IDisposable
{
    protected ApiTestBase(bool useFileDatabase = false)
    {
        Factory = new TestApiFactory(useFileDatabase);
        Client = Factory.CreateClient();
    }

    protected TestApiFactory Factory { get; }

    protected HttpClient Client { get; }

    protected static async Task<JsonElement> JsonOf(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    protected async Task<int> CreateTicketAsync(string title = "Unable to access my account")
    {
        using var response = await Client.PostAsJsonAsync(
            "/api/tickets",
            new { title, description = "I reset my password but still cannot sign in." });
        response.EnsureSuccessStatusCode();
        return (await JsonOf(response)).GetProperty("id").GetInt32();
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
