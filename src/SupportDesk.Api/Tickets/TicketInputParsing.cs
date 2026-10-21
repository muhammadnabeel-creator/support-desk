using System.Globalization;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Tickets;

/// <summary>
/// Input rules shared by the REST controller and the MCP tools (contract C6, FR15.1), so an id or enum value
/// from an LLM client is checked exactly as strictly as one from an HTTP request.
/// </summary>
public static class TicketInputParsing
{
    private const string IdMessage = "Id must be a positive whole number.";

    /// <summary>Parses a ticket id from text: digits only, at least 1 (BR3.2).</summary>
    /// <param name="id">The raw id, for example a route value.</param>
    /// <returns>The id.</returns>
    /// <exception cref="ValidationFailedException">The text is not a positive whole number.</exception>
    public static int ParseId(string? id)
    {
        if (int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value >= 1)
        {
            return value;
        }

        throw new ValidationFailedException("id", IdMessage);
    }

    /// <summary>Checks a numeric ticket id: at least 1 (BR3.2).</summary>
    /// <param name="id">The id.</param>
    /// <returns>The same id.</returns>
    /// <exception cref="ValidationFailedException">The id is 0 or negative.</exception>
    public static int ValidateId(int id) =>
        id >= 1 ? id : throw new ValidationFailedException("id", IdMessage);

    /// <summary>
    /// Parses an optional enum value by name, ignoring case (BR2.2). Empty or whitespace means "not given";
    /// numbers and unknown names are recorded as an error listing the allowed values (BR2.3).
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The raw value.</param>
    /// <param name="field">The camelCase field name used in the error, for example <c>status</c>.</param>
    /// <param name="errors">Collects the error when the value is invalid.</param>
    /// <returns>The parsed value, or <c>null</c> when it was empty or invalid.</returns>
    public static TEnum? ParseOptionalEnum<TEnum>(string? value, string field, IDictionary<string, string[]> errors)
        where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(errors);

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (StrictEnum.TryParse<TEnum>(value.Trim(), out var parsed))
        {
            return parsed;
        }

        var label = char.ToUpperInvariant(field[0]) + field[1..];
        errors[field] = [$"{label} is not valid. Allowed values: {StrictEnum.AllowedValues<TEnum>()}."];
        return null;
    }

    /// <summary>
    /// Parses one enum value by name and throws when it is present but invalid. A missing value is returned as
    /// <c>null</c> so the service reports it as missing, as it does for REST requests.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The raw value.</param>
    /// <param name="field">The camelCase field name, for example <c>priority</c>.</param>
    /// <returns>The parsed value, or <c>null</c> when it was empty.</returns>
    /// <exception cref="ValidationFailedException">The value is not one of the enum's names.</exception>
    public static TEnum? ParseEnumOrThrow<TEnum>(string? value, string field)
        where TEnum : struct, Enum
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var parsed = ParseOptionalEnum<TEnum>(value, field, errors);
        return errors.Count > 0 ? throw new ValidationFailedException(errors) : parsed;
    }
}
