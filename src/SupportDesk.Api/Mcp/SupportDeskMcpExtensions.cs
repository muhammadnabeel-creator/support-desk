using ModelContextProtocol.Protocol;

namespace SupportDesk.Api.Mcp;

/// <summary>
/// The SupportDesk MCP setup shared by both transports (ADR-006): the server name, the eight tools and the
/// filter that turns unreadable tool arguments into a 400 error result. Used by the stdio host (<c>--mcp</c>)
/// and by the Streamable HTTP endpoint at <c>/mcp</c> inside the web API.
/// </summary>
public static class SupportDeskMcpExtensions
{
    /// <summary>Registers the MCP server with the SupportDesk tools; the caller adds the transport.</summary>
    /// <param name="services">The service collection (the shared services must also be registered).</param>
    /// <returns>The MCP server builder, for adding a transport.</returns>
    public static IMcpServerBuilder AddSupportDeskMcp(this IServiceCollection services) =>
        services
            .AddMcpServer(options => options.ServerInfo = new Implementation { Name = "supportdesk", Version = "1.0" })
            .WithTools<TicketTools>()
            .WithRequestFilters(filters => filters.AddCallToolFilter(next => async (context, cancellationToken) =>
            {
                // Our tools turn every failure into an error result with structured content. An error result
                // without it comes from the SDK itself, which happens when the arguments could not be bound
                // (wrong type or missing value): report that as a 400 in the shared error shape (contract C6).
                try
                {
                    var result = await next(context, cancellationToken);
                    return result.IsError == true && result.StructuredContent is null
                        ? InvalidArguments()
                        : result;
                }
                catch (Exception exception) when (exception is ArgumentException or FormatException
                    or System.Text.Json.JsonException or InvalidCastException)
                {
                    return InvalidArguments();
                }
            }));

    private static CallToolResult InvalidArguments() => McpToolResults.Error(
        StatusCodes.Status400BadRequest,
        McpToolResults.InvalidArgumentsTitle,
        "Check the argument names and types against the tool's input schema.");
}
