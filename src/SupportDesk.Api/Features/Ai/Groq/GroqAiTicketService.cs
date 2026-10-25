using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Features.Ai.Configuration;
using SupportDesk.Api.Features.Ai.Groq;
using SupportDesk.Api.Features.Ai.Groq.Dtos;
using SupportDesk.Api.Features.Ai.Models;
using SupportDesk.Api.Features.Ai.Validation;

namespace SupportDesk.Api.Features.Ai.Groq;

/// <summary>
/// The Groq adapter for <see cref="IAiTicketService"/> (contracts C2 and C4), used as a typed <see cref="HttpClient"/>.
/// The client's <c>BaseAddress</c> is the configured base URL with a trailing slash and its own timeout is infinite;
/// this class applies the configured timeout per call (BR10.2).
/// <para>
/// Failures are classified into the four typed AI exceptions (NFR3.8): no key gives
/// <see cref="AiNotConfiguredException"/> before any network call (BR10.1); our timeout gives
/// <see cref="AiTimeoutException"/>; a network error, a non-2xx status (including 429) or an unreadable or empty
/// envelope gives <see cref="AiProviderException"/> (BR10.3); truncated or invalid content gives
/// <see cref="AiInvalidOutputException"/>. When the caller's token is cancelled (the client went away), the
/// <see cref="OperationCanceledException"/> is rethrown unchanged. There are no retries (BR10.4).
/// </para>
/// <para>
/// Exception messages are fixed and generic and never contain provider text, ticket text, the URL or the key
/// (NFR2.15). Each call writes exactly one log line with the operation, model, outcome, duration and HTTP status
/// (BR10.5, NFR2.18).
/// </para>
/// </summary>
public sealed class GroqAiTicketService : IAiTicketService
{
    /// <summary>The message of every <see cref="AiNotConfiguredException"/> raised here.</summary>
    public const string NotConfiguredMessage = "The AI provider is not configured.";

    /// <summary>The message of every <see cref="AiTimeoutException"/> raised here.</summary>
    public const string TimeoutMessage = "The AI provider did not respond in time.";

    /// <summary>The message of every <see cref="AiProviderException"/> raised here.</summary>
    public const string ProviderErrorMessage = "The AI provider returned an error.";

    /// <summary>
    /// The chat completions path, relative to the base address. It has no leading slash, so the base URL's
    /// path (<c>/openai/v1/</c>) is kept.
    /// </summary>
    public const string ChatCompletionsPath = "chat/completions";

    /// <summary>
    /// Token limit for an analysis; the JSON answer is small (NFR3.4). Raised from 400 after review R-02 so
    /// answers in non-Latin scripts, which use more tokens, are not cut off.
    /// </summary>
    public const int AnalysisMaxTokens = 600;

    /// <summary>
    /// Token limit for a draft (NFR3.4). Raised from 700 after review R-02; the prompt still asks for under
    /// 2,000 characters and validation still caps the draft at 4,000.
    /// </summary>
    public const int DraftMaxTokens = 900;

    /// <summary>Sampling temperature for both operations; low values keep answers consistent (NFR3.4).</summary>
    public const double Temperature = 0.2;

    /// <summary>The operation name logged for <see cref="AnalyzeAsync"/>.</summary>
    public const string AnalyzeOperation = "analyze";

    /// <summary>The operation name logged for <see cref="DraftReplyAsync"/>.</summary>
    public const string DraftReplyOperation = "draft-reply";

    private const string BearerScheme = "Bearer";
    private const string OkOutcome = "ok";
    private const string NotConfiguredOutcome = "not-configured";
    private const string TimeoutOutcome = "timeout";
    private const string ProviderErrorOutcome = "provider-error";
    private const string InvalidOutputOutcome = "invalid-output";
    private const string CancelledOutcome = "cancelled";

    private readonly HttpClient _httpClient;
    private readonly IOptions<AiOptions> _options;
    private readonly ILogger<GroqAiTicketService> _logger;

    /// <summary>Creates the adapter.</summary>
    /// <param name="httpClient">The typed client, with <c>BaseAddress</c> set and an infinite timeout.</param>
    /// <param name="options">The AI settings; read on every call, so a missing key is detected per request.</param>
    /// <param name="logger">Logger for the one line written per call.</param>
    public GroqAiTicketService(HttpClient httpClient, IOptions<AiOptions> options, ILogger<GroqAiTicketService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<AiAnalysisResult> AnalyzeAsync(string title, string description, CancellationToken cancellationToken)
    {
        var messages = new[]
        {
            new GroqMessage { Role = GroqMessage.SystemRole, Content = AiPrompts.AnalysisSystemMessage },
            new GroqMessage { Role = GroqMessage.UserRole, Content = AiPrompts.BuildAnalysisUserMessage(title, description) },
        };

        return SendAsync(
            AnalyzeOperation,
            messages,
            AnalysisMaxTokens,
            GroqResponseFormat.JsonObject,
            AiOutputValidator.ValidateAnalysis,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<AiDraftResult> DraftReplyAsync(
        string title,
        string description,
        AiDraftContext? analysis,
        CancellationToken cancellationToken)
    {
        var messages = new[]
        {
            new GroqMessage { Role = GroqMessage.SystemRole, Content = AiPrompts.DraftSystemMessage },
            new GroqMessage
            {
                Role = GroqMessage.UserRole,
                Content = AiPrompts.BuildDraftUserMessage(title, description, analysis),
            },
        };

        // No response_format: a draft is plain text.
        return SendAsync(
            DraftReplyOperation,
            messages,
            DraftMaxTokens,
            responseFormat: null,
            AiOutputValidator.ValidateDraft,
            cancellationToken);
    }

    /// <summary>
    /// Builds the client's base address from the configured base URL, adding a trailing slash so that the
    /// relative <see cref="ChatCompletionsPath"/> is appended to the URL's path instead of replacing its last
    /// segment (<c>https://api.groq.com/openai/v1</c> becomes <c>https://api.groq.com/openai/v1/</c>).
    /// </summary>
    /// <param name="baseUrl">The configured base URL; must be an absolute URL (checked at startup).</param>
    /// <returns>The absolute base address ending with a slash.</returns>
    public static Uri CreateBaseAddress(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        return new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    /// <summary>
    /// Sends one chat request, classifies any failure and validates the content. Writes one log line in all cases.
    /// </summary>
    private async Task<TResult> SendAsync<TResult>(
        string operation,
        IReadOnlyList<GroqMessage> messages,
        int maxTokens,
        GroqResponseFormat? responseFormat,
        Func<string, TResult> validate,
        CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var stopwatch = Stopwatch.StartNew();
        var outcome = OkOutcome;
        int? statusCode = null;

        try
        {
            if (!options.IsConfigured)
            {
                throw new AiNotConfiguredException(NotConfiguredMessage);
            }

            var body = new GroqChatRequest
            {
                Model = options.Groq.Model,
                Messages = messages,
                Temperature = Temperature,
                MaxTokens = maxTokens,
                ResponseFormat = responseFormat,
            };

            // Our own timeout, linked to the caller's token so a client disconnect still cancels the call.
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsPath)
                {
                    Content = JsonContent.Create(body),
                };

                // Per request, never on DefaultRequestHeaders, so the key lives only as long as this message (NFR2.14).
                request.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, options.Groq.ApiKey);

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutSource.Token);
                statusCode = (int)response.StatusCode;

                if (!response.IsSuccessStatusCode)
                {
                    // The provider's error body is never read or passed on (NFR2.15).
                    throw new AiProviderException(ProviderErrorMessage);
                }

                var envelope = await ReadEnvelopeAsync(response, timeoutSource.Token);
                var content = ExtractContent(envelope);
                return validate(content);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The client went away; the global handler turns this into 499.
                throw;
            }
            catch (OperationCanceledException exception)
            {
                throw new AiTimeoutException(TimeoutMessage, exception);
            }
            catch (HttpRequestException exception)
            {
                throw new AiProviderException(ProviderErrorMessage, exception);
            }
        }
        catch (Exception exception)
        {
            outcome = ClassifyOutcome(exception);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            LogCall(operation, options.Groq.Model, outcome, stopwatch.ElapsedMilliseconds, statusCode);
        }
    }

    /// <summary>Reads and deserialises the response envelope; unreadable or malformed bodies are provider errors.</summary>
    private static async Task<GroqChatResponse?> ReadEnvelopeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<GroqChatResponse>(stream, cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            // The parser's message can quote the body, so it is not attached (NFR2.15).
            throw new AiProviderException(ProviderErrorMessage);
        }
        catch (IOException)
        {
            // A read that broke because the call was cancelled is a cancellation (client gone or our timeout),
            // not a provider error.
            cancellationToken.ThrowIfCancellationRequested();
            throw new AiProviderException(ProviderErrorMessage);
        }
    }

    /// <summary>
    /// Takes the first choice's content. A missing choice or empty content is a provider error; an answer cut off at
    /// <c>max_tokens</c> is invalid output (NFR3.4).
    /// </summary>
    private static string ExtractContent(GroqChatResponse? envelope)
    {
        var choice = envelope?.Choices is { Count: > 0 } choices ? choices[0] : null;
        var content = choice?.Message?.Content;
        if (choice is null || string.IsNullOrEmpty(content))
        {
            throw new AiProviderException(ProviderErrorMessage);
        }

        if (string.Equals(choice.FinishReason, GroqChoice.LengthFinishReason, StringComparison.Ordinal))
        {
            throw new AiInvalidOutputException(AiOutputValidator.InvalidOutputMessage);
        }

        return content;
    }

    private static string ClassifyOutcome(Exception exception) => exception switch
    {
        AiNotConfiguredException => NotConfiguredOutcome,
        AiTimeoutException => TimeoutOutcome,
        AiInvalidOutputException => InvalidOutputOutcome,
        OperationCanceledException => CancelledOutcome,
        _ => ProviderErrorOutcome,
    };

    /// <summary>Writes the one line per call. It never contains the key, the ticket text or the AI text.</summary>
    private void LogCall(string operation, string model, string outcome, long elapsedMs, int? statusCode)
    {
        var level = outcome == OkOutcome ? LogLevel.Information : LogLevel.Warning;
        _logger.Log(
            level,
            "AI {Operation} via {Model} -> {Outcome} in {ElapsedMs} ms (HTTP {StatusCode})",
            operation,
            model,
            outcome,
            elapsedMs,
            statusCode);
    }
}
