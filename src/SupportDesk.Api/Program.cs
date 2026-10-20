using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
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
