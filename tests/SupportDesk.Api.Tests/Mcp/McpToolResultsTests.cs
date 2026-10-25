using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Mcp;

namespace SupportDesk.Api.Tests.Mcp;

/// <summary>Tool results: REST-shaped JSON on success, safe error results on failure (FR14.3, FR14.4, FR15.3).</summary>
[Trait("Unit", "U5")]
public sealed class McpToolResultsTests
{
    private static JsonElement TextJson(CallToolResult result) =>
        JsonDocument.Parse(Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text).RootElement;

    [Fact]
    public void Success_UsesCamelCaseAndEnumNames()
    {
        var result = McpToolResults.Success(new { TicketId = 3, Status = Domain.TicketStatus.InProgress });

        Assert.NotEqual(true, result.IsError);
        var json = TextJson(result);
        Assert.Equal(3, json.GetProperty("ticketId").GetInt32());
        Assert.Equal("InProgress", json.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Object, result.StructuredContent!.Value.ValueKind);
    }

    [Fact]
    public void Success_WrapsAListForStructuredContent()
    {
        var result = McpToolResults.Success(new[] { 1, 2 });

        Assert.Equal(JsonValueKind.Array, TextJson(result).ValueKind);
        Assert.Equal(2, result.StructuredContent!.Value.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task ValidationFailure_BecomesA400ErrorResult_WithFieldErrors()
    {
        var result = await McpToolResults.RunAsync<object>(
            () => throw new ValidationFailedException("title", "Title is required."),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.True(result.IsError);
        var json = TextJson(result);
        Assert.Equal(400, json.GetProperty("status").GetInt32());
        Assert.Equal("Title is required.", json.GetProperty("errors").GetProperty("title")[0].GetString());
    }

    [Theory]
    [InlineData(typeof(AiTimeoutException), 504)]
    [InlineData(typeof(AiProviderException), 502)]
    [InlineData(typeof(AiNotConfiguredException), 503)]
    public async Task AiFailures_KeepTheirStatus(Type exceptionType, int status)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "x")!;

        var result = await McpToolResults.RunAsync<object>(() => throw exception, NullLogger.Instance, CancellationToken.None);

        Assert.Equal(status, TextJson(result).GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task UnexpectedFailure_IsAGeneric500_WithoutDetails()
    {
        var result = await McpToolResults.RunAsync<object>(
            () => throw new InvalidOperationException("stack trace and secrets here"),
            NullLogger.Instance,
            CancellationToken.None);

        var text = Assert.IsType<TextContentBlock>(result.Content[0]).Text;
        Assert.True(result.IsError);
        Assert.Contains(McpToolResults.UnexpectedDetail, text);
        Assert.DoesNotContain("secrets", text);
        Assert.DoesNotContain("InvalidOperationException", text);
    }

    [Fact]
    public async Task CallerCancellation_IsRethrown_NotTurnedIntoAResult()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => McpToolResults.RunAsync<object>(
            () => throw new OperationCanceledException(cancelled.Token),
            NullLogger.Instance,
            cancelled.Token));
    }
}
