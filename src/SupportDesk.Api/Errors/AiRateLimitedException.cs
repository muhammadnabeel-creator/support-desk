namespace SupportDesk.Api.Errors;

/// <summary>
/// Raised when the AI request budget (<c>Ai:MaxRequestsPerMinute</c>) is used up. Mapped to 429 with a
/// <c>Retry-After</c> header for REST; the AI provider is not called and nothing is saved.
/// </summary>
public class AiRateLimitedException : Exception
{
    /// <summary>Creates the failure.</summary>
    /// <param name="retryAfter">How long until the next AI request is allowed.</param>
    public AiRateLimitedException(TimeSpan retryAfter)
        : base("The AI request limit was reached.")
    {
        RetryAfter = retryAfter;
    }

    /// <summary>How long until the next AI request is allowed.</summary>
    public TimeSpan RetryAfter { get; }

    /// <summary>The wait in whole seconds, at least 1, for the <c>Retry-After</c> header and messages.</summary>
    public int RetryAfterSeconds => Math.Max(1, (int)Math.Ceiling(RetryAfter.TotalSeconds));
}
