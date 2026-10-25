using System.Net;
using System.Text;
using System.Text.Json;

namespace SupportDesk.Api.Tests.Api;

/// <summary>
/// The MCP endpoint served by the running API at <c>/mcp</c> (FR13, FR14): a real JSON-RPC client exchange over
/// Streamable HTTP, through the same services, error mapping and argument filter as the stdio mode.
/// </summary>
[Trait("Unit", "U5")]
public sealed class McpHttpApiTests : ApiTestBase
{
    private async Task<JsonElement> RpcAsync(string method, object? @params = null)
    {
        var body = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, method, @params });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");

        using var response = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The reply may be plain JSON or one Server-Sent Event ("data: {...}").
        var text = await response.Content.ReadAsStringAsync();
        var json = text.Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith("data:", StringComparison.Ordinal))?["data:".Length..]
            ?? text;
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private static JsonElement ToolJson(JsonElement reply) =>
        JsonDocument.Parse(reply.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()!).RootElement;

    [Fact]
    public async Task ToolsList_ReturnsTheEightTools()
    {
        var reply = await RpcAsync("tools/list");

        var names = reply.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString())
            .ToList();
        Assert.Equal(8, names.Count);
        Assert.Contains("analyze_ticket", names);
        Assert.Contains("draft_reply", names);
    }

    [Fact]
    public async Task CreateTicket_ThroughMcp_IsVisibleThroughRest()
    {
        var reply = await RpcAsync("tools/call", new
        {
            name = "create_ticket",
            arguments = new { title = "Created via MCP", description = "A ticket created by an MCP client." },
        });

        var id = ToolJson(reply).GetProperty("id").GetInt32();
        var rest = await JsonOf(await Client.GetAsync($"/api/tickets/{id}"));
        Assert.Equal("Created via MCP", rest.GetProperty("title").GetString());
    }

    [Fact]
    public async Task WrongArgumentType_IsA400ToolError_NotAProtocolError()
    {
        var reply = await RpcAsync("tools/call", new { name = "get_ticket", arguments = new { id = "abc" } });

        Assert.True(reply.GetProperty("result").GetProperty("isError").GetBoolean());
        Assert.Equal(400, ToolJson(reply).GetProperty("status").GetInt32());
        Assert.False(reply.TryGetProperty("error", out _));
    }
}
