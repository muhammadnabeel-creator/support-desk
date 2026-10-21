using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SupportDesk.Api.Infrastructure;

/// <summary>
/// The JSON settings shared by the REST API and the MCP tools (contract C1 conventions, C6), so both return
/// the same field names and enum spellings.
/// </summary>
public static class JsonDefaults
{
    /// <summary>Ready-made options for serializing results outside MVC (used by the MCP tools).</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    /// <summary>Applies the shared settings to <paramref name="options"/>.</summary>
    /// <param name="options">The options to configure.</param>
    public static void Apply(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        // Enums travel as names only (contract C1). The strict converter comes first and wins: it also
        // rejects numeric strings ("1") and combinations ("Open,Closed"), which the built-in converter
        // would accept. The built-in one, with integers disabled, stays as the documented default.
        options.Converters.Add(new StrictEnumConverterFactory());
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        Apply(options);
        options.MakeReadOnly();
        return options;
    }
}
