using System.Text.Json;
using ModelContextProtocol.Protocol;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Mcp;

/// <summary>
/// Turns service results and failures into MCP tool results (contract C5, FR14.3, FR14.4). Success is the same
/// JSON as the REST response. A failure is a tool error result (<c>isError: true</c>) whose JSON carries the same
/// <c>status</c>, <c>title</c>, <c>detail</c> and per-field <c>errors</c> as the REST ProblemDetails, from the
/// shared <see cref="ProblemMapping"/>; never a stack trace, exception type, key or provider text (FR15.3).
/// </summary>
public static class McpToolResults
{
    /// <summary>Detail of an unexpected (500) failure; the REST text without the traceId sentence.</summary>
    public const string UnexpectedDetail = "The server could not complete the request.";

    /// <summary>Title of a tool call whose arguments could not be read (wrong type, missing value).</summary>
    public const string InvalidArgumentsTitle = "The tool arguments could not be read.";

    /// <summary>
    /// Runs one tool body and converts its outcome. Every expected failure is a typed exception from the
    /// services; anything else is logged to standard error and returned as a generic 500.
    /// </summary>
    /// <typeparam name="T">The response type, for example a ticket response.</typeparam>
    /// <param name="body">The tool body: one service call plus the REST response mapping.</param>
    /// <param name="logger">Logs unexpected failures (standard error only in MCP mode).</param>
    /// <param name="cancellationToken">The tool call's cancellation; a cancelled call returns nothing.</param>
    /// <returns>The tool result.</returns>
    public static async Task<CallToolResult> RunAsync<T>(
        Func<Task<T>> body,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            return Success(await body());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The client cancelled the call: nothing was saved, and nobody is waiting for a result.
            throw;
        }
        catch (Exception exception)
        {
            var problem = ProblemMapping.Create(exception);
            var status = problem.Status ?? StatusCodes.Status500InternalServerError;
            if (status == StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "MCP tool failed unexpectedly.");
            }
            else if (status >= StatusCodes.Status500InternalServerError)
            {
                // AI failures: the type is enough; the adapter already logged its own line without content.
                logger.LogWarning("MCP tool failed with {ExceptionType} ({StatusCode}).", exception.GetType().Name, status);
            }

            var errors = problem is HttpValidationProblemDetails validation ? validation.Errors : null;
            var detail = status == StatusCodes.Status500InternalServerError ? UnexpectedDetail : problem.Detail;
            return Error(status, problem.Title ?? ProblemMapping.UnexpectedTitle, detail ?? string.Empty, errors);
        }
    }

    /// <summary>Builds a successful result whose text and structured content are the JSON of <paramref name="value"/>.</summary>
    /// <typeparam name="T">The response type.</typeparam>
    /// <param name="value">The response.</param>
    /// <returns>The tool result.</returns>
    public static CallToolResult Success<T>(T value)
    {
        var element = JsonSerializer.SerializeToElement(value, JsonDefaults.Options);
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = element.GetRawText() }],
            StructuredContent = element.ValueKind == JsonValueKind.Object ? element : Wrap("items", element),
        };
    }

    /// <summary>Builds a tool error result in the shared error shape.</summary>
    /// <param name="status">The HTTP-style status, for example 400 or 504.</param>
    /// <param name="title">The short, safe title.</param>
    /// <param name="detail">The short, safe detail.</param>
    /// <param name="errors">Per-field validation messages, or <c>null</c>.</param>
    /// <returns>The tool result with <c>isError</c> set.</returns>
    public static CallToolResult Error(
        int status,
        string title,
        string detail,
        IDictionary<string, string[]>? errors = null)
    {
        var body = new Dictionary<string, object?>
        {
            ["status"] = status,
            ["title"] = title,
            ["detail"] = detail,
        };
        if (errors is { Count: > 0 })
        {
            body["errors"] = errors;
        }

        var element = JsonSerializer.SerializeToElement(body, JsonDefaults.Options);
        return new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = element.GetRawText() }],
            StructuredContent = element,
        };
    }

    // MCP structured content must be a JSON object, so a list result is wrapped as { "items": [...] }.
    private static JsonElement Wrap(string name, JsonElement value) =>
        JsonSerializer.SerializeToElement(new Dictionary<string, JsonElement> { [name] = value }, JsonDefaults.Options);
}
