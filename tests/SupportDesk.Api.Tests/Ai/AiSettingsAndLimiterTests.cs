using Microsoft.Extensions.Options;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Features.Ai;
using SupportDesk.Api.Features.Ai.Configuration;

namespace SupportDesk.Api.Tests.Ai;

/// <summary>AI settings never reveal the key (NFR2.14), and the AI request budget is enforced (rate limiting).</summary>
[Trait("Unit", "U2")]
public sealed class AiSettingsAndLimiterTests
{
    [Fact]
    public void ToString_NeverPrintsTheKey()
    {
        var options = new AiOptions { Groq = new GroqOptions { ApiKey = "gsk_super_secret_value" } };

        Assert.DoesNotContain("gsk_super_secret_value", options.ToString());
        Assert.Contains("(set, hidden)", options.ToString());
        Assert.Contains("(not set)", new AiOptions().ToString());
    }

    [Fact]
    public void Key_IsTrimmed_AndBlankMeansNotConfigured()
    {
        Assert.Equal("abc", new GroqOptions { ApiKey = "  abc\n" }.ApiKey);
        Assert.Equal(string.Empty, new GroqOptions { ApiKey = null! }.ApiKey);
        Assert.False(new AiOptions { Groq = new GroqOptions { ApiKey = "   " } }.IsConfigured);
        Assert.True(new AiOptions { Groq = new GroqOptions { ApiKey = "abc" } }.IsConfigured);
    }

    [Fact]
    public void Limiter_AllowsTheBudget_ThenRefusesWithARetryTime()
    {
        using var limiter = new FixedWindowAiCallLimiter(Options.Create(new AiOptions { MaxRequestsPerMinute = 3 }));

        limiter.Acquire();
        limiter.Acquire();
        limiter.Acquire();
        var failure = Assert.Throws<AiRateLimitedException>(limiter.Acquire);

        Assert.InRange(failure.RetryAfterSeconds, 1, 60);
    }

    [Fact]
    public async Task Limiter_IsSafeUnderParallelCalls()
    {
        using var limiter = new FixedWindowAiCallLimiter(Options.Create(new AiOptions { MaxRequestsPerMinute = 10 }));
        var allowed = 0;

        await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            try
            {
                limiter.Acquire();
                Interlocked.Increment(ref allowed);
            }
            catch (AiRateLimitedException)
            {
            }
        })));

        Assert.Equal(10, allowed);
    }

    [Fact]
    public void RetryAfterSeconds_IsAtLeastOne()
    {
        Assert.Equal(1, new AiRateLimitedException(TimeSpan.FromMilliseconds(10)).RetryAfterSeconds);
        Assert.Equal(31, new AiRateLimitedException(TimeSpan.FromSeconds(30.2)).RetryAfterSeconds);
    }
}
