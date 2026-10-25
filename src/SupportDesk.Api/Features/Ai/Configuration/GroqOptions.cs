namespace SupportDesk.Api.Features.Ai.Configuration;

/// <summary>
/// Groq settings, bound from <c>Ai:Groq</c> as part of <see cref="AiOptions"/> (contract C4).
/// <see cref="ToString"/> never prints the API key.
/// </summary>
public sealed class GroqOptions
{
    /// <summary>Default for <see cref="Model"/>.</summary>
    public const string DefaultModel = "openai/gpt-oss-120b";

    /// <summary>Default for <see cref="BaseUrl"/> (Groq's OpenAI-compatible API).</summary>
    public const string DefaultBaseUrl = "https://api.groq.com/openai/v1";

    /// <summary>
    /// The Groq API key. Set only through user secrets or environment variables, never in committed files
    /// (NFR2.13). Empty means the AI provider is not configured. Surrounding whitespace (for example a
    /// newline pasted with the key) is trimmed, so it never reaches the Authorization header.
    /// </summary>
    public string ApiKey
    {
        get => _apiKey;
        set => _apiKey = value?.Trim() ?? string.Empty;
    }

    private string _apiKey = string.Empty;

    /// <summary>The Groq model name.</summary>
    public string Model { get; set; } = DefaultModel;

    /// <summary>The base URL of the chat completions API; must be an absolute <c>https</c> URL.</summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>Describes the settings for diagnostics without revealing the API key.</summary>
    /// <returns>The model and base URL, with the key shown only as set or not set.</returns>
    public override string ToString()
    {
        var keyState = string.IsNullOrWhiteSpace(ApiKey) ? "(not set)" : "(set, hidden)";
        return $"GroqOptions {{ ApiKey = {keyState}, Model = {Model}, BaseUrl = {BaseUrl} }}";
    }
}
