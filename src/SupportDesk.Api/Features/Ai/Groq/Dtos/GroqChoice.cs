using System.Text.Json.Serialization;

namespace SupportDesk.Api.Features.Ai.Groq.Dtos;

/// <summary>
/// One choice in a chat completion response (contract C4).
/// </summary>
public sealed record GroqChoice
{
    /// <summary>The finish reason that means the answer was cut off at <c>max_tokens</c>.</summary>
    public const string LengthFinishReason = "length";

    /// <summary>The generated message.</summary>
    [JsonPropertyName("message")]
    public GroqMessage? Message { get; init; }

    /// <summary>Why generation stopped, for example <c>stop</c> or <c>length</c> (truncated).</summary>
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; init; }
}
