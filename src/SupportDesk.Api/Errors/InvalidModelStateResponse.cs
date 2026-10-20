using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Errors;

/// <summary>
/// Builds the 400 response for requests that fail before the action runs: a body that is not valid JSON,
/// a value of the wrong JSON type, an unknown or numeric enum value, or a missing required field
/// (BR4.1, BR5.2, BR6.3). Register as <c>ApiBehaviorOptions.InvalidModelStateResponseFactory</c>.
/// <para>
/// The response has the same shape as a <see cref="ValidationFailedException"/> written by
/// <see cref="GlobalExceptionHandler"/>: status 400, the same title and detail, an <c>errors</c> map keyed by
/// camelCase field names, and a <c>traceId</c>. Parser messages are replaced with fixed text, so CLR type
/// names and parser internals never reach the client; an invalid enum value lists the allowed values.
/// </para>
/// </summary>
public static class InvalidModelStateResponse
{
    /// <summary>The <c>errors</c> key used when the problem is the body as a whole.</summary>
    public const string BodyField = "body";

    private const string MissingBodyMessage = "A JSON request body is required.";
    private const string InvalidBodyMessage = "The request body is not valid JSON.";
    private const string FallbackMessage = "The value is not valid.";
    private const string JsonPathPrefix = "$.";

    /// <summary>Creates the 400 ProblemDetails result for the invalid model state in <paramref name="context"/>.</summary>
    /// <param name="context">The action context whose model state is invalid.</param>
    /// <returns>A 400 result with an <c>application/problem+json</c> validation body.</returns>
    public static IActionResult Create(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelState = new ModelStateDictionary();
        foreach (var (field, messages) in CollectErrors(context))
        {
            foreach (var message in messages)
            {
                modelState.AddModelError(field, message);
            }
        }

        // The framework factory adds the standard "type" link and the traceId, and runs the same
        // ProblemDetails customisation as the exception handler, so both 400 paths look identical.
        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(
            context.HttpContext,
            modelState,
            StatusCodes.Status400BadRequest,
            GlobalExceptionHandler.ValidationTitle,
            type: null,
            GlobalExceptionHandler.ValidationDetail,
            instance: null);

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" },
        };
    }

    /// <summary>Turns the raw model state into field (camelCase) to messages, without duplicates.</summary>
    private static Dictionary<string, List<string>> CollectErrors(ActionContext context)
    {
        var bodyParameter = context.ActionDescriptor.Parameters
            .FirstOrDefault(p => p.BindingInfo?.BindingSource == BindingSource.Body);
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var (key, entry) in context.ModelState)
        {
            if (entry.Errors.Count == 0
                || (bodyParameter is not null && string.Equals(key, bodyParameter.Name, StringComparison.OrdinalIgnoreCase)))
            {
                // The "<parameter> field is required" entry only repeats a body problem reported under
                // another key; if it is the only entry, the fallback below reports a missing body.
                continue;
            }

            if (key.Length == 0)
            {
                Add(errors, BodyField, MissingBodyMessage);
            }
            else if (key == "$")
            {
                Add(errors, BodyField, InvalidBodyMessage);
            }
            else if (key.StartsWith(JsonPathPrefix, StringComparison.Ordinal))
            {
                var field = ToCamelCase(key[JsonPathPrefix.Length..]);
                Add(errors, field, DescribeJsonError(field, bodyParameter?.ParameterType));
            }
            else
            {
                foreach (var error in entry.Errors)
                {
                    Add(errors, ToCamelCase(key), string.IsNullOrEmpty(error.ErrorMessage) ? FallbackMessage : error.ErrorMessage);
                }
            }
        }

        if (errors.Count == 0)
        {
            Add(errors, BodyField, MissingBodyMessage);
        }

        return errors;
    }

    /// <summary>
    /// Message for a value the JSON reader rejected. For an enum property it lists the allowed names;
    /// otherwise it says the value is malformed or of the wrong type.
    /// </summary>
    private static string DescribeJsonError(string field, Type? bodyType)
    {
        var label = char.ToUpperInvariant(field[0]) + field[1..];
        var property = bodyType?.GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, field, StringComparison.OrdinalIgnoreCase));
        var propertyType = property is null
            ? null
            : Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        return propertyType is { IsEnum: true }
            ? $"{label} is not valid. Allowed values: {StrictEnum.AllowedValues(propertyType)}."
            : $"{label} is not valid JSON or has the wrong type.";
    }

    private static string ToCamelCase(string name) =>
        name.Length == 0 || char.IsLower(name[0]) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    private static void Add(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var messages))
        {
            messages = [];
            errors[field] = messages;
        }

        if (!messages.Contains(message, StringComparer.Ordinal))
        {
            messages.Add(message);
        }
    }
}
