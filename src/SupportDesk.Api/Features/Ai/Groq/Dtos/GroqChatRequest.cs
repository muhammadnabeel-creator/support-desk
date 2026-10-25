using System.Text.Json.Serialization;

namespace SupportDesk.Api.Features.Ai.Groq.Dtos;

/// <summary>
/// Request body for Groq's OpenAI-compatible <c>POST chat/completions</c> (contract C4, NFR8.12).
/// JSON names are snake_case as the API expects.
/// </summary>
public sealed record GroqChatRequest
{
    /// <summary>The model name, from <c>Ai:Groq:Model</c>.</summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>The system message followed by the user message.</summary>
    [JsonPropertyName("messages")]
    public required IReadOnlyList<GroqMessage> Messages { get; init; }

    /// <summary>Sampling temperature; low values keep answers consistent (NFR3.4).</summary>
    [JsonPropertyName("temperature")]
    public required double Temperature { get; init; }

    /// <summary>Upper limit on generated tokens, which keeps responses small (NFR3.4).</summary>
    [JsonPropertyName("max_tokens")]
    public required int MaxTokens { get; init; }

    /// <summary>JSON mode for analysis requests; left out of the body when null (draft requests).</summary>
    [JsonPropertyName("response_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GroqResponseFormat? ResponseFormat { get; init; }
}
