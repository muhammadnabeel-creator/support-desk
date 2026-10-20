using Microsoft.OpenApi.Models;
using SupportDesk.Api.Domain;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SupportDesk.Api.Infrastructure;

/// <summary>
/// Makes the Swagger document describe parameters as contract C1 does. The <c>{id}</c> route value and the
/// list filters are bound as strings so the actions can return a clear 400 for bad input (BR2.3, BR3.2);
/// without this filter Swagger would show them as free text. This filter changes documentation only:
/// <c>id</c> is shown as an integer with minimum 1, and the <c>status</c>, <c>priority</c> and <c>category</c>
/// query parameters as their enums (names only).
/// </summary>
public sealed class ApiParameterSchemaFilter : IOperationFilter
{
    private static readonly Dictionary<string, Type> _queryEnumParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["status"] = typeof(TicketStatus),
        ["priority"] = typeof(TicketPriority),
        ["category"] = typeof(TicketCategory),
    };

    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);
        if (operation.Parameters is null)
        {
            return;
        }

        foreach (var parameter in operation.Parameters)
        {
            if (parameter.In == ParameterLocation.Path && string.Equals(parameter.Name, "id", StringComparison.OrdinalIgnoreCase))
            {
                parameter.Schema = new OpenApiSchema { Type = "integer", Format = "int32", Minimum = 1 };
            }
            else if (parameter.In == ParameterLocation.Query && _queryEnumParameters.TryGetValue(parameter.Name, out var enumType))
            {
                parameter.Schema = context.SchemaGenerator.GenerateSchema(enumType, context.SchemaRepository);
            }
        }
    }
}
