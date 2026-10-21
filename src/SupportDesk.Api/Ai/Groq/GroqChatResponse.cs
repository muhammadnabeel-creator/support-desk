using System.Text.Json.Serialization;

namespace SupportDesk.Api.Ai.Groq;

/// <summary>
/// The part of Groq's chat completion response that we read (contract C4): <c>choices[0].message.content</c>
/// and <c>finish_reason</c>. Every member is nullable because the envelope comes from outside and is checked
/// before use; other fields are ignored.
/// </summary>
public sealed record GroqChatResponse
{
    /// <summary>The generated choices; only the first one is used.</summary>
    [JsonPropertyName("choices")]
    public IReadOnlyList<GroqChoice>? Choices { get; init; }
}
