using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Api.Errors;

namespace SupportDesk.Api.Tests.Errors;

/// <summary>One mapping from failures to status, title and detail, shared by REST and MCP (FR-09, ADR-005, C6).</summary>
[Trait("Unit", "U1")]
public sealed class ProblemMappingTests
{
    public static TheoryData<Exception, int> Mappings() => new()
    {
        { new ValidationFailedException("title", "Title is required."), 400 },
        { new TicketNotFoundException(7), 404 },
        { new AiInvalidOutputException("x"), 502 },
        { new AiProviderException("x"), 502 },
        { new AiNotConfiguredException("x"), 503 },
        { new AiTimeoutException("x"), 504 },
        { new AiRateLimitedException(TimeSpan.FromSeconds(12)), 429 },
        { new BadHttpRequestException("x", StatusCodes.Status413PayloadTooLarge), 413 },
        { new InvalidOperationException("internal detail"), 500 },
    };

    [Theory]
    [MemberData(nameof(Mappings))]
    public void EachFailure_HasItsStatus_AndATitleAndDetail(Exception exception, int status)
    {
        var problem = ProblemMapping.Create(exception);

        Assert.Equal(status, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
        Assert.False(string.IsNullOrWhiteSpace(problem.Detail));
    }

    [Fact]
    public void Validation_CarriesTheFieldErrors()
    {
        var problem = Assert.IsType<HttpValidationProblemDetails>(
            ProblemMapping.Create(new ValidationFailedException("status", "Status is required.")));

        Assert.Equal(["Status is required."], problem.Errors["status"]);
        Assert.Equal(ProblemMapping.ValidationTitle, problem.Title);
    }

    [Fact]
    public void NotFound_NamesTheId_AndRateLimit_SaysWhenToRetry()
    {
        Assert.Contains("7", ProblemMapping.Create(new TicketNotFoundException(7)).Detail);
        Assert.Contains("12 seconds", ProblemMapping.Create(new AiRateLimitedException(TimeSpan.FromSeconds(12))).Detail);
    }

    [Fact]
    public void UnexpectedFailure_NeverRevealsItsMessageOrType()
    {
        var problem = ProblemMapping.Create(new InvalidOperationException("Connection string=secret"));

        Assert.Equal(ProblemMapping.UnexpectedTitle, problem.Title);
        Assert.DoesNotContain("secret", problem.Detail);
        Assert.DoesNotContain("InvalidOperationException", problem.Detail);
    }

    [Fact]
    public void ValidationFailedException_NeedsAtLeastOneError()
    {
        Assert.Throws<ArgumentException>(() => new ValidationFailedException(new Dictionary<string, string[]>()));
    }
}
