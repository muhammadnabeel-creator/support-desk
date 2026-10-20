using System.Text.Json;
using System.Text.Json.Serialization;

namespace SupportDesk.Api.Infrastructure;

/// <summary>
/// Reads an enum only from a JSON string that names a defined member (case-insensitive) and writes
/// the member name. JSON numbers (<c>1</c>), numeric strings (<c>"1"</c>), combinations and unknown
/// names fail with a <see cref="JsonException"/>, which MVC turns into a 400 for that field (BR5.2, BR6.3).
/// Created per enum type by <see cref="StrictEnumConverterFactory"/>.
/// </summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
public sealed class StrictEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    /// <inheritdoc />
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && StrictEnum.TryParse<TEnum>(reader.GetString(), out var value))
        {
            return value;
        }

        throw new JsonException($"Expected one of: {StrictEnum.AllowedValues<TEnum>()}.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.ToString());
    }
}
