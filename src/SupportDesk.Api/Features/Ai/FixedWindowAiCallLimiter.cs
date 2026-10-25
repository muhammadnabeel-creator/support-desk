using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Features.Ai.Configuration;

namespace SupportDesk.Api.Features.Ai;

/// <summary>
/// The AI request budget as a fixed one-minute window (.NET's built-in <see cref="FixedWindowRateLimiter"/>):
/// at most <c>Ai:MaxRequestsPerMinute</c> AI requests start per minute; extra requests are refused at once (no
/// queue). Registered as a singleton, so all requests in this process share the budget; the limiter is thread-safe.
/// </summary>
public sealed class FixedWindowAiCallLimiter : IAiCallLimiter, IDisposable
{
    private static readonly TimeSpan _window = TimeSpan.FromMinutes(1);

    private readonly FixedWindowRateLimiter _limiter;

    /// <summary>Creates the limiter from the AI settings.</summary>
    /// <param name="options">The AI settings; <see cref="AiOptions.MaxRequestsPerMinute"/> sets the budget.</param>
    public FixedWindowAiCallLimiter(IOptions<AiOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.Value.MaxRequestsPerMinute,
            Window = _window,
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    }

    /// <inheritdoc />
    public void Acquire()
    {
        using var lease = _limiter.AttemptAcquire();
        if (!lease.IsAcquired)
        {
            var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var wait) ? wait : _window;
            throw new AiRateLimitedException(retryAfter);
        }
    }

    /// <inheritdoc />
    public void Dispose() => _limiter.Dispose();
}
