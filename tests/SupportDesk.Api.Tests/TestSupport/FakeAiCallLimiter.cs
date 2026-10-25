using SupportDesk.Api.Errors;
using SupportDesk.Api.Features.Ai;

namespace SupportDesk.Api.Tests.TestSupport;

/// <summary>An AI budget tests control: it allows every call unless <see cref="Allow"/> is turned off.</summary>
public sealed class FakeAiCallLimiter : IAiCallLimiter
{
    public bool Allow { get; set; } = true;

    public int Calls { get; private set; }

    public void Acquire()
    {
        Calls++;
        if (!Allow)
        {
            throw new AiRateLimitedException(TimeSpan.FromSeconds(30));
        }
    }
}
