using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Mcp;

/// <summary>
/// Runs the API project as an MCP server over stdio when started with <c>--mcp</c> (FR13, ADR-006): the transport
/// the brief requires, for clients that start their own server process. It uses the same services, configuration
/// and database as the web API, and nothing listens on the network. (The running web API also serves MCP over
/// HTTP at <c>/mcp</c>; see <c>Program.cs</c>.)
/// </summary>
public static class McpServerHost
{
    /// <summary>The command-line switch that selects MCP mode.</summary>
    public const string Switch = "--mcp";

    private const string ProjectFileName = "SupportDesk.Api.csproj";

    /// <summary>Builds and runs the MCP host until the client closes the connection.</summary>
    /// <param name="args">The process arguments; <see cref="Switch"/> is removed before configuration reads them.</param>
    /// <returns>A task that completes when the server stops.</returns>
    public static async Task RunAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args.Where(arg => arg != Switch).ToArray(),

            // The client may start the process from any folder. Using the API project folder as content root
            // makes appsettings.json and the "App_Data/supportdesk.db" path resolve exactly as for the web API.
            ContentRootPath = FindProjectFolder() ?? AppContext.BaseDirectory,
        });

        // MCP clients start the process outside Development, where user secrets are not loaded by default. Load
        // them explicitly (local single-user process) so the Groq key set with `dotnet user-secrets` works here
        // too; environment variables are added again afterwards so they still win (ADR-006).
        builder.Configuration.AddUserSecrets<Program>(optional: true);
        builder.Configuration.AddEnvironmentVariables();

        // Standard output carries MCP messages only, so every log line goes to standard error (FR13.4).
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

        builder.Services.AddSupportDeskServices();
        builder.Services.AddSupportDeskMcp().WithStdioServerTransport();

        await builder.Build().RunAsync();
    }

    /// <summary>
    /// Walks up from the running assembly's folder (for example <c>bin/Debug/net8.0</c>) to the folder that
    /// holds the API project file; <c>null</c> when there is none (for example a published copy).
    /// </summary>
    private static string? FindProjectFolder()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, ProjectFileName)))
            {
                return folder.FullName;
            }
        }

        return null;
    }
}
