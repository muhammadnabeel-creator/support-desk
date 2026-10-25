using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.OpenApi.Models;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Infrastructure;
using SupportDesk.Api.Mcp;

// `--mcp` runs the same services as an MCP server over stdio instead of the web API (FR13, ADR-006).
if (args.Contains(McpServerHost.Switch))
{
    await McpServerHost.RunAsync(args);
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers(options =>
    {
        // Validation errors are keyed by the JSON (camelCase) property name, e.g. "title", not "Title".
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
    })
    .AddJsonOptions(options =>
    {
        // camelCase names and strict enum names (contract C1); shared with the MCP tools.
        JsonDefaults.Apply(options.JsonSerializerOptions);

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

// Database, clock, ticket and dashboard services, and the validated AI settings with the Groq client. Shared
// with the --mcp host so REST controllers and MCP tools use the same implementations (ADR-006, contract C6).
builder.Services.AddSupportDeskServices();

// MCP over Streamable HTTP at /mcp, always on while the API runs: the same eight tools and services as the
// stdio mode (--mcp), so an MCP client can connect to the running API by URL (FR13, ADR-006 amendment).
builder.Services.AddSupportDeskMcp().WithHttpTransport(options => options.Stateless = true);

// Errors: one exception handler turns every failure into ProblemDetails with a traceId (BR6.1-BR6.4).
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsCustomizer.Customize);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// GlobalExceptionHandler logs each failure once at the right level; the built-in middleware would
// log every exception again at Error (including 400s and 404s), so its own logging is silenced.
builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.None);

var app = builder.Build();

// Registered in every environment; the developer exception page is never used (NFR2.1, NFR2.2).
app.UseExceptionHandler();

// Unknown routes (404) and wrong methods (405) get a ProblemDetails body too, not an empty response (BR6.1).
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapMcp("/mcp");

app.Run();

/// <summary>
/// Entry point marker. Declared partial and public so the test project's
/// <c>WebApplicationFactory&lt;Program&gt;</c> can start the application.
/// </summary>
public partial class Program
{
}
