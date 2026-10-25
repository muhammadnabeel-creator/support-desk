using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SupportDesk.Api.Features.Ai;
using SupportDesk.Api.Features.Ai.Configuration;
using SupportDesk.Api.Features.Ai.Groq;
using SupportDesk.Api.Features.Dashboard;
using SupportDesk.Api.Features.Tickets;

namespace SupportDesk.Api.Infrastructure;

/// <summary>
/// Registers the application's services once for both hosts: the web API and the <c>--mcp</c> MCP server
/// (ADR-006, contract C6). REST controllers and MCP tools therefore resolve exactly the same implementations.
/// </summary>
public static class SupportDeskServiceCollectionExtensions
{
    /// <summary>
    /// Adds the database context, clock, ticket and dashboard services, and the validated AI settings with the
    /// Groq typed HTTP client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddSupportDeskServices(this IServiceCollection services)
    {
        // SQLite through EF Core. The relative Data Source is resolved under the content root when the
        // context is first created; the schema comes only from migrations (no Migrate/EnsureCreated here).
        services.AddDbContext<SupportDeskDbContext>((provider, options) =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var environment = provider.GetRequiredService<IHostEnvironment>();
            var connectionString = DatabasePath.ResolveConnectionString(
                configuration.GetConnectionString("SupportDesk"),
                environment.ContentRootPath);
            options.UseSqlite(connectionString);
        });
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IDashboardService, DashboardService>();

        // AI settings (WF9, NFR3.10, NFR4.3). A bad endpoint, model or timeout stops startup with a message naming
        // the key. The API key is deliberately not validated: without it the app still starts, ticket operations
        // work, and the AI operations fail per request as "not configured" (BR10.1, NFR1.3).
        services.AddOptions<AiOptions>()
            .BindConfiguration(AiOptions.SectionName)
            .Validate(
                options => Uri.TryCreate(options.Groq.BaseUrl, UriKind.Absolute, out var baseUri)
                    && baseUri.Scheme == Uri.UriSchemeHttps,
                "Ai:Groq:BaseUrl must be an absolute https URL.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Groq.Model),
                "Ai:Groq:Model must not be empty.")
            .Validate(
                options => options.TimeoutSeconds is >= AiOptions.MinTimeoutSeconds and <= AiOptions.MaxTimeoutSeconds,
                $"Ai:TimeoutSeconds must be between {AiOptions.MinTimeoutSeconds} and {AiOptions.MaxTimeoutSeconds}.")
            .Validate(
                options => options.MaxRequestsPerMinute is >= AiOptions.MinMaxRequestsPerMinute
                    and <= AiOptions.MaxMaxRequestsPerMinute,
                $"Ai:MaxRequestsPerMinute must be between {AiOptions.MinMaxRequestsPerMinute} and {AiOptions.MaxMaxRequestsPerMinute}.")
            .ValidateOnStart();

        // One AI request budget for the whole process (REST and MCP share it), so the Groq quota is protected.
        services.AddSingleton<IAiCallLimiter, FixedWindowAiCallLimiter>();

        // Groq through a typed, pooled HttpClient (NFR3.3). The base address keeps its "/openai/v1" path thanks to
        // the trailing slash; the client's own timeout is off because GroqAiTicketService applies
        // Ai:TimeoutSeconds per call. No retry handler is added (BR10.4).
        services.AddHttpClient<IAiTicketService, GroqAiTicketService>((provider, client) =>
        {
            var aiOptions = provider.GetRequiredService<IOptions<AiOptions>>().Value;
            client.BaseAddress = GroqAiTicketService.CreateBaseAddress(aiOptions.Groq.BaseUrl);
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        return services;
    }
}
