namespace SupportDesk.Api.Ai;

/// <summary>
/// Settings bound from the <c>Ai</c> configuration section (WF9). The API key comes only from
/// <c>dotnet user-secrets</c> or the <c>Ai__Groq__ApiKey</c> environment variable; committed files hold an
/// empty placeholder (NFR2.13). A missing key does not stop the app: AI requests then fail as
/// "not configured" (BR10.1). <see cref="ToString"/> never prints the key.
/// </summary>
public sealed class AiOptions
{
    /// <summary>The configuration section these settings are bound from.</summary>
    public const string SectionName = "Ai";

    /// <summary>Default for <see cref="TimeoutSeconds"/>.</summary>
    public const int DefaultTimeoutSeconds = 20;

    /// <summary>Smallest allowed <see cref="TimeoutSeconds"/>.</summary>
    public const int MinTimeoutSeconds = 1;

    /// <summary>Largest allowed <see cref="TimeoutSeconds"/>.</summary>
    public const int MaxTimeoutSeconds = 120;

    /// <summary>Groq provider settings (<c>Ai:Groq</c>).</summary>
    public GroqOptions Groq { get; set; } = new();

    /// <summary>How long one AI request may take before it fails as a timeout (BR10.2). Allowed range 1-120.</summary>
    public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;

    /// <summary>True when an API key is set (not empty or whitespace).</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Groq.ApiKey);

    /// <summary>Describes the settings for diagnostics without revealing the API key.</summary>
    /// <returns>The settings, with the key shown only as set or not set.</returns>
    public override string ToString() => $"AiOptions {{ Groq = {Groq}, TimeoutSeconds = {TimeoutSeconds} }}";
}
