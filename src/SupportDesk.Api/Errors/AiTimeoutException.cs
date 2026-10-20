namespace SupportDesk.Api.Errors;

/// <summary>
/// Raised (by U2) when an AI request does not complete within its configured timeout.
/// Mapped to 504 (contract C3).
/// </summary>
public class AiTimeoutException : Exception
{
    /// <summary>Creates the failure with a server-side message (never shown to clients).</summary>
    /// <param name="message">Which request timed out and after how long.</param>
    public AiTimeoutException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the failure with a server-side message and the underlying cancellation.</summary>
    /// <param name="message">Which request timed out and after how long.</param>
    /// <param name="innerException">The cancellation or timeout exception.</param>
    public AiTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
