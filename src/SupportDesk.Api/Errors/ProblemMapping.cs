using Microsoft.AspNetCore.Mvc;

namespace SupportDesk.Api.Errors;

/// <summary>
/// The one mapping from exceptions to error responses (BR6.1, BR6.2, ADR-005), shared by the REST exception
/// handler and the MCP tools (contract C6) so both report every failure with the same status, title and detail.
/// Typed failures map to fixed codes: validation 400, ticket not found 404, invalid AI output 502, AI provider
/// error 502, AI not configured 503, AI timeout 504. Anything else is a generic 500 that never carries a stack
/// trace, exception type or exception message.
/// </summary>
public static class ProblemMapping
{
    /// <summary>The ProblemDetails title of every validation (400) response.</summary>
    public const string ValidationTitle = "One or more validation errors occurred.";

    /// <summary>The ProblemDetails detail of every validation (400) response.</summary>
    public const string ValidationDetail = "See the errors property for the fields that failed validation.";

    /// <summary>The title of every unexpected (500) failure.</summary>
    public const string UnexpectedTitle = "An unexpected error occurred.";

    /// <summary>Builds the error response for <paramref name="exception"/>.</summary>
    /// <param name="exception">The failure.</param>
    /// <returns>A ProblemDetails with status, title and detail set (and <c>errors</c> for validation).</returns>
    public static ProblemDetails Create(Exception exception) => exception switch
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
            UnexpectedTitle,
            "The server could not complete the request. Quote the traceId when reporting this problem."),
    };

    private static ProblemDetails Problem(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };
}
