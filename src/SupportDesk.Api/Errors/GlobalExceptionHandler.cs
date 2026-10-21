using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SupportDesk.Api.Errors;

/// <summary>
/// The one place that writes exceptions as ProblemDetails HTTP responses (BR6.1, BR6.2, BR6.4, ADR-005); the
/// status, title and detail come from <see cref="ProblemMapping"/>, which the MCP tools share.
/// Typed failures map to fixed status codes: validation 400, ticket not found 404, invalid AI output 502,
/// AI provider error 502, AI not configured 503, AI timeout 504. Anything else becomes a generic 500 that
/// never carries a stack trace, exception type or exception message, and is logged once at Error with the
/// request's <c>TraceId</c> (NFR2.10). Validation failures are not logged; not-found is logged at Information.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    /// <summary>The ProblemDetails title of every validation (400) response.</summary>
    public const string ValidationTitle = ProblemMapping.ValidationTitle;

    /// <summary>The ProblemDetails detail of every validation (400) response.</summary>
    public const string ValidationDetail = ProblemMapping.ValidationDetail;

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

        var problem = ProblemMapping.Create(exception);
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
                // Type name only: the exception object (stack, inner messages) is never logged (NFR2.19).
                _logger.LogWarning(
                    "AI request failed with {ExceptionType} and returned {StatusCode}. TraceId {TraceId}",
                    exception.GetType().Name,
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
