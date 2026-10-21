using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SupportDesk.Api.Ai;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Infrastructure;
using SupportDesk.Api.Tickets;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers(options =>
    {
        // Validation errors are keyed by the JSON (camelCase) property name, e.g. "title", not "Title".
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
    })
    .AddJsonOptions(options =>
    {
        var json = options.JsonSerializerOptions;
        json.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        // Enums travel as names only (contract C1). The strict converter comes first and wins: it also
        // rejects numeric strings ("1") and combinations ("Open,Closed"), which the built-in converter
        // would accept. The built-in one, with integers disabled, stays as the documented default.
        json.Converters.Add(new StrictEnumConverterFactory());
        json.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));

        // Parser messages (with CLR type names and byte positions) never reach the client.
        options.AllowInputFormatterExceptionMessages = false;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Model-binding failures (bad JSON, bad enum, missing required field) use the same
        // ProblemDetails shape as ValidationFailedException (BR6.1, BR6.3).
        options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SupportDesk AI API",
        Version = "1.0",
        Description = "Support tickets with AI-assisted analysis and draft replies. For local use only; there is no authentication.",
    });

    // GenerateDocumentationFile is on, so the XML comments ship next to the assembly.
    var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    options.IncludeXmlComments(xmlFile);
    options.OperationFilter<ApiParameterSchemaFilter>();
});

// SQLite through EF Core. The relative Data Source is resolved under the content root when the
// context is first created; the schema comes only from migrations (no Migrate/EnsureCreated here).
builder.Services.AddDbContext<SupportDeskDbContext>((services, options) =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var environment = services.GetRequiredService<IHostEnvironment>();
    var connectionString = DatabasePath.ResolveConnectionString(
        configuration.GetConnectionString("SupportDesk"),
        environment.ContentRootPath);
    options.UseSqlite(connectionString);
});
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<ITicketService, TicketService>();

// AI settings (WF9, NFR3.10, NFR4.3). A bad endpoint, model or timeout stops startup with a message naming the
// key. The API key is deliberately not validated: without it the app still starts, ticket operations work, and
// the AI endpoints answer 503 per request (BR10.1, NFR1.3).
builder.Services.AddOptions<AiOptions>()
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
    .ValidateOnStart();

// Groq through a typed, pooled HttpClient (NFR3.3). The base address keeps its "/openai/v1" path thanks to the
// trailing slash; the client's own timeout is off because GroqAiTicketService applies Ai:TimeoutSeconds per call.
// No retry handler is added (BR10.4).
builder.Services.AddHttpClient<IAiTicketService, GroqAiTicketService>((services, client) =>
{
    var aiOptions = services.GetRequiredService<IOptions<AiOptions>>().Value;
    client.BaseAddress = GroqAiTicketService.CreateBaseAddress(aiOptions.Groq.BaseUrl);
    client.Timeout = Timeout.InfiniteTimeSpan;
});

// Errors: one exception handler turns every failure into ProblemDetails with a traceId (BR6.1-BR6.4).
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsCustomizer.Customize);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// GlobalExceptionHandler logs each failure once at the right level; the built-in middleware would
// log every exception again at Error (including 400s and 404s), so its own logging is silenced.
builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.None);

var app = builder.Build();

// Registered in every environment; the developer exception page is never used (NFR2.1, NFR2.2).
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Entry point marker. Declared partial and public so the test project's
/// <c>WebApplicationFactory&lt;Program&gt;</c> can start the application.
/// </summary>
public partial class Program
{
}
