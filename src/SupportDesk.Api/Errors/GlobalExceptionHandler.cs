using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SupportDesk.Api.Errors;

/// <summary>
/// The one place that turns exceptions into ProblemDetails responses (BR6.1, BR6.2, BR6.4, ADR-005).
/// Typed failures map to fixed status codes: validation 400, ticket not found 404, invalid AI output 502,
/// AI provider error 502, AI not configured 503, AI timeout 504. Anything else becomes a generic 500 that
/// never carries a stack trace, exception type or exception message, and is logged once at Error with the
/// request's <c>TraceId</c> (NFR2.10). Validation failures are not logged; not-found is logged at Information.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    /// <summary>The ProblemDetails title of every validation (400) response.</summary>
    public const string ValidationTitle = "One or more validation errors occurred.";

    /// <summary>The ProblemDetails detail of every validation (400) response.</summary>
    public const string ValidationDetail = "See the errors property for the fields that failed validation.";

    private const string ProblemJsonContentType = "application/problem+json";

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>Creates the handler.</summary>
    /// <param name="problemDetailsService">Writes ProblemDetails bodies (registered by <c>AddProblemDetails</c>).</param>
    /// <param name="logger">Logger for unexpected and noteworthy failures.</param>
    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = ProblemDetailsCustomizer.GetTraceId(httpContext);

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // The client went away; there is nobody to send a body to and nothing went wrong on our side.
            _logger.LogInformation("Request was cancelled by the client. TraceId {TraceId}", traceId);
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        var problem = CreateProblemDetails(exception);
        LogFailure(exception, problem.Status!.Value, traceId);

        httpContext.Response.StatusCode = problem.Status.Value;
        var written = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
        });

        if (!written)
        {
            // No registered writer accepted the request's Accept header; write problem+json anyway.
            problem.Extensions[ProblemDetailsCustomizer.TraceIdKey] = traceId;
            await httpContext.Response.WriteAsJsonAsync(
                problem,
                problem.GetType(),
                (JsonSerializerOptions?)null,
                ProblemJsonContentType,
                cancellationToken);
        }

        return true;
    }

    private static ProblemDetails CreateProblemDetails(Exception exception) => exception switch
    {
        ValidationFailedException validation => new HttpValidationProblemDetails(
            new Dictionary<string, string[]>(validation.Errors))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = ValidationTitle,
            Detail = ValidationDetail,
        },
        TicketNotFoundException notFound => Problem(
            StatusCodes.Status404NotFound,
            "Ticket not found.",
            $"Ticket {notFound.TicketId} was not found."),
        AiInvalidOutputException => Problem(
            StatusCodes.Status502BadGateway,
            "The AI response was invalid.",
            "The AI provider returned output that could not be validated. Nothing was saved."),
        AiProviderException => Problem(
            StatusCodes.Status502BadGateway,
            "The AI provider failed.",
            "The AI provider could not be reached or returned an error. Try again later."),
        AiNotConfiguredException => Problem(
            StatusCodes.Status503ServiceUnavailable,
            "AI features are not configured.",
            "The AI provider is not configured on this server. Ticket operations still work."),
        AiTimeoutException => Problem(
            StatusCodes.Status504GatewayTimeout,
            "The AI provider timed out.",
            "The AI provider did not respond in time. Nothing was saved. Try again later."),
        BadHttpRequestException badRequest => Problem(
            badRequest.StatusCode,
            "The request could not be read.",
            "The request was malformed or could not be processed."),
        _ => Problem(
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request. Quote the traceId when reporting this problem."),
    };

    private static ProblemDetails Problem(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };

    private void LogFailure(Exception exception, int statusCode, string traceId)
    {
        switch (exception)
        {
            case ValidationFailedException:
                // Client error: deliberately not logged (NFR2.11).
                break;
            case TicketNotFoundException notFound:
                _logger.LogInformation(
                    "Ticket {TicketId} was not found. TraceId {TraceId}",
                    notFound.TicketId,
                    traceId);
                break;
            case AiInvalidOutputException or AiProviderException or AiNotConfiguredException or AiTimeoutException:
                _logger.LogWarning(
                    exception,
                    "AI request failed and returned {StatusCode}. TraceId {TraceId}",
                    statusCode,
                    traceId);
                break;
            case BadHttpRequestException:
                _logger.LogInformation(
                    "Bad request returned {StatusCode}. TraceId {TraceId}",
                    statusCode,
                    traceId);
                break;
            default:
                _logger.LogError(exception, "Unhandled exception. TraceId {TraceId}", traceId);
                break;
        }
    }
}
