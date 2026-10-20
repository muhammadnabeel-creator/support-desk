using System.Text.Json;
using System.Text.Json.Serialization;

namespace SupportDesk.Api.Infrastructure;

/// <summary>
/// Supplies a <see cref="StrictEnumConverter{TEnum}"/> for every enum type, so all enums in requests
/// and responses travel as member names (contract C1). Nullable enums are handled by System.Text.Json,
/// which wraps this converter and passes JSON <c>null</c> through as <c>null</c>.
/// Register it before <see cref="JsonStringEnumConverter"/> so it takes precedence.
/// </summary>
public sealed class StrictEnumConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        return typeToConvert.IsEnum;
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(StrictEnumConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
