namespace SupportDesk.Api.Errors;

/// <summary>
/// Raised (by U2) when the AI provider's output cannot be parsed or fails validation against the
/// typed DTO and allowed values. Nothing is saved. Mapped to 502 (contract C3).
/// </summary>
public class AiInvalidOutputException : Exception
{
    /// <summary>Creates the failure with a server-side message (never shown to clients).</summary>
    /// <param name="message">Why the output was rejected; must not contain secrets.</param>
    public AiInvalidOutputException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the failure with a server-side message and the underlying cause.</summary>
    /// <param name="message">Why the output was rejected; must not contain secrets.</param>
    /// <param name="innerException">The parsing or validation error.</param>
    public AiInvalidOutputException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
