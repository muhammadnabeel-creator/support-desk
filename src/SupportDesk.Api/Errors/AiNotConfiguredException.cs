namespace SupportDesk.Api.Errors;

/// <summary>
/// Raised (by U2) when an AI feature is requested but the provider is not configured
/// (for example, no API key). Mapped to 503 (contract C3).
/// </summary>
public class AiNotConfiguredException : Exception
{
    /// <summary>Creates the failure with a server-side message (never shown to clients).</summary>
    /// <param name="message">Which setting is missing; must not contain secret values.</param>
    public AiNotConfiguredException(string message)
        : base(message)
    {
    }
}
