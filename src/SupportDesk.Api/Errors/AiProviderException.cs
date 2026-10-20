namespace SupportDesk.Api.Errors;

/// <summary>
/// Raised (by U2) when the AI provider returns an HTTP error or cannot be reached.
/// Mapped to 502 (contract C3).
/// </summary>
public class AiProviderException : Exception
{
    /// <summary>Creates the failure with a server-side message (never shown to clients).</summary>
    /// <param name="message">What went wrong; must not contain secrets or raw provider responses.</param>
    public AiProviderException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the failure with a server-side message and the underlying cause.</summary>
    /// <param name="message">What went wrong; must not contain secrets or raw provider responses.</param>
    /// <param name="innerException">The HTTP or network error.</param>
    public AiProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
