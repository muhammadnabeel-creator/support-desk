using System.Text.Json.Serialization;

namespace SupportDesk.Api.Ai.Groq;

/// <summary>
/// One chat message, used in requests and read from responses (contract C4).
/// </summary>
public sealed record GroqMessage
{
    /// <summary>Role name for the instructions message.</summary>
    public const string SystemRole = "system";

    /// <summary>Role name for the ticket data message.</summary>
    public const string UserRole = "user";

    /// <summary>The message role: <c>system</c>, <c>user</c> or (in responses) <c>assistant</c>.</summary>
    [JsonPropertyName("role")]
    public string? Role { get; init; }

    /// <summary>The message text. In a response it is untrusted AI output and must be validated before use.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; init; }
}
