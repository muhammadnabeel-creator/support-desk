using System.Diagnostics;

namespace SupportDesk.Api.Errors;

/// <summary>
/// Adds a <c>traceId</c> to every ProblemDetails body (NFR2.12). The same id is used in the
/// error log line written by <see cref="GlobalExceptionHandler"/>, so a 500 can be matched to its log entry.
/// Register with <c>services.AddProblemDetails(o =&gt; o.CustomizeProblemDetails = ProblemDetailsCustomizer.Customize)</c>.
/// </summary>
public static class ProblemDetailsCustomizer
{
    /// <summary>The ProblemDetails extension key that carries the trace id.</summary>
    public const string TraceIdKey = "traceId";

    /// <summary>Adds the request's trace id to the ProblemDetails being written.</summary>
    /// <param name="context">The ProblemDetails write context.</param>
    public static void Customize(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.ProblemDetails.Extensions[TraceIdKey] = GetTraceId(context.HttpContext);
    }

    /// <summary>
    /// The trace id for the request: the current activity id when tracing is active,
    /// otherwise the ASP.NET Core request identifier.
    /// </summary>
    /// <param name="httpContext">The current request.</param>
    /// <returns>A non-empty trace id.</returns>
    public static string GetTraceId(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return Activity.Current?.Id ?? httpContext.TraceIdentifier;
    }
}
