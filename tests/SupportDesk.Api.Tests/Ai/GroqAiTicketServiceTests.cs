using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Features.Ai.Configuration;
using SupportDesk.Api.Features.Ai.Groq;
using SupportDesk.Api.Tests.TestSupport;

namespace SupportDesk.Api.Tests.Ai;

/// <summary>
/// The Groq adapter with the network replaced by a stub (FR-09, BR10.1-BR10.5, NFR3): every failure becomes one of
/// the typed AI errors, the key is sent only on the request, and our own timeout applies. Groq is never called.
/// </summary>
[Trait("Unit", "U2")]
public sealed class GroqAiTicketServiceTests
{
    private const string ValidAnalysis =
        """{"category":"Billing","priority":"Medium","sentiment":"Neutral","summary":"Charged twice.","suggestedAction":"Check the invoice."}""";

    private static GroqAiTicketService Create(StubHttpMessageHandler handler, string apiKey = "test-key", int timeoutSeconds = 5)
    {
        var options = new AiOptions
        {
            Groq = new GroqOptions { ApiKey = apiKey },
            TimeoutSeconds = timeoutSeconds,
        };
        var client = new HttpClient(handler)
        {
            BaseAddress = GroqAiTicketService.CreateBaseAddress(options.Groq.BaseUrl),
            Timeout = Timeout.InfiniteTimeSpan,
        };
        return new GroqAiTicketService(client, Options.Create(options), NullLogger<GroqAiTicketService>.Instance);
    }

    [Fact]
    public async Task Analyze_SendsAJsonModeRequest_AndReturnsTheValidatedResult()
    {
        var handler = StubHttpMessageHandler.GroqReply(ValidAnalysis);

        var result = await Create(handler).AnalyzeAsync("Double charge", "I was billed twice.", CancellationToken.None);

        Assert.Equal(TicketCategory.Billing, result.Category);
        Assert.Equal("https://api.groq.com/openai/v1/chat/completions", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("test-key", handler.LastRequest.Headers.Authorization.Parameter);

        using var body = JsonDocument.Parse(handler.LastBody!);
        Assert.Equal(GroqOptions.DefaultModel, body.RootElement.GetProperty("model").GetString());
        Assert.Equal(GroqAiTicketService.AnalysisMaxTokens, body.RootElement.GetProperty("max_tokens").GetInt32());
        Assert.Equal("json_object", body.RootElement.GetProperty("response_format").GetProperty("type").GetString());
        Assert.Equal(2, body.RootElement.GetProperty("messages").GetArrayLength());
    }

    [Fact]
    public async Task Draft_SendsAPlainTextRequest_AndReturnsTheTrimmedDraft()
    {
        var handler = StubHttpMessageHandler.GroqReply("  Hello, we are looking into it.  ");

        var result = await Create(handler).DraftReplyAsync("Title", "A description.", null, CancellationToken.None);

        Assert.Equal("Hello, we are looking into it.", result.Draft);
        using var body = JsonDocument.Parse(handler.LastBody!);
        Assert.Equal(GroqAiTicketService.DraftMaxTokens, body.RootElement.GetProperty("max_tokens").GetInt32());
        Assert.False(body.RootElement.TryGetProperty("response_format", out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NoKey_IsNotConfigured_AndMakesNoNetworkCall(string apiKey)
    {
        var handler = StubHttpMessageHandler.GroqReply(ValidAnalysis);

        await Assert.ThrowsAsync<AiNotConfiguredException>(
            () => Create(handler, apiKey).AnalyzeAsync("Title", "A description.", CancellationToken.None));

        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ErrorStatus_IsAProviderError_WithoutTheProviderText(HttpStatusCode status)
    {
        var handler = StubHttpMessageHandler.Json(status, """{"error":{"message":"secret provider detail"}}""");

        var failure = await Assert.ThrowsAsync<AiProviderException>(
            () => Create(handler).AnalyzeAsync("Title", "A description.", CancellationToken.None));

        Assert.DoesNotContain("secret provider detail", failure.Message);
    }

    [Theory]
    [InlineData("""{"choices":[]}""")]
    [InlineData("""{"choices":[{"message":{"role":"assistant","content":""},"finish_reason":"stop"}]}""")]
    [InlineData("""{}""")]
    [InlineData("not json")]
    public async Task UnusableEnvelope_IsAProviderError(string body)
    {
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, body);

        await Assert.ThrowsAsync<AiProviderException>(
            () => Create(handler).AnalyzeAsync("Title", "A description.", CancellationToken.None));
    }

    [Fact]
    public async Task NetworkFailure_IsAProviderError()
    {
        var handler = new StubHttpMessageHandler((_, _) => throw new HttpRequestException("connection refused"));

        await Assert.ThrowsAsync<AiProviderException>(
            () => Create(handler).DraftReplyAsync("Title", "A description.", null, CancellationToken.None));
    }

    [Fact]
    public async Task CutOffAnswer_IsInvalidOutput()
    {
        var handler = StubHttpMessageHandler.GroqReply(ValidAnalysis, finishReason: "length");

        await Assert.ThrowsAsync<AiInvalidOutputException>(
            () => Create(handler).AnalyzeAsync("Title", "A description.", CancellationToken.None));
    }

    [Fact]
    public async Task ContentThatFailsValidation_IsInvalidOutput()
    {
        var handler = StubHttpMessageHandler.GroqReply("""{"category":"Food","priority":"Medium"}""");

        await Assert.ThrowsAsync<AiInvalidOutputException>(
            () => Create(handler).AnalyzeAsync("Title", "A description.", CancellationToken.None));
    }

    [Fact]
    public async Task SlowProvider_TimesOut()
    {
        var handler = new StubHttpMessageHandler(async (_, token) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await Assert.ThrowsAsync<AiTimeoutException>(
            () => Create(handler, timeoutSeconds: 1).AnalyzeAsync("Title", "A description.", CancellationToken.None));
    }

    [Fact]
    public async Task CallerCancellation_IsNotReportedAsATimeout()
    {
        var handler = new StubHttpMessageHandler(async (_, token) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var caller = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Create(handler, timeoutSeconds: 10).AnalyzeAsync("Title", "A description.", caller.Token));

        Assert.IsNotType<AiTimeoutException>(failure);
    }

    [Theory]
    [InlineData("https://api.groq.com/openai/v1", "https://api.groq.com/openai/v1/")]
    [InlineData("https://api.groq.com/openai/v1/", "https://api.groq.com/openai/v1/")]
    public void BaseAddress_KeepsTheV1Path(string configured, string expected)
    {
        Assert.Equal(expected, GroqAiTicketService.CreateBaseAddress(configured).ToString());
    }
}
