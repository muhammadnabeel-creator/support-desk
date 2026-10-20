using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SupportDesk.Api.Tests;

/// <summary>
/// Proves the test runner can start the API in memory through
/// <see cref="WebApplicationFactory{TEntryPoint}"/> (Development environment).
/// </summary>
[Trait("Unit", "U1")]
public sealed class SmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SwaggerDocument_IsServed_InDevelopment()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"openapi\"", body);
    }

    [Fact]
    public async Task UnknownRoute_Returns404()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
