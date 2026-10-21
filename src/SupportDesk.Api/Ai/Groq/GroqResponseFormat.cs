using System.Text.Json.Serialization;

namespace SupportDesk.Api.Ai.Groq;

/// <summary>
/// The <c>response_format</c> of a chat request (contract C4). Analysis requests use JSON mode.
/// </summary>
public sealed record GroqResponseFormat
{
    /// <summary>The format type that asks the model for a single JSON object.</summary>
    public const string JsonObjectType = "json_object";

    /// <summary>The format type, for example <c>json_object</c>.</summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>A response format requesting a single JSON object.</summary>
    public static GroqResponseFormat JsonObject { get; } = new() { Type = JsonObjectType };
}
