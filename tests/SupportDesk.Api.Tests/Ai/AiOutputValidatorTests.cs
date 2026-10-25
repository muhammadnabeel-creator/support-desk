using SupportDesk.Api.Domain;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Features.Ai.Validation;

namespace SupportDesk.Api.Tests.Ai;

/// <summary>
/// AI output is untrusted (FR-06, BR8.1-BR8.4, BR9.1): only a JSON object with allowed values and sane lengths
/// becomes a result; anything else is rejected before it can be saved.
/// </summary>
[Trait("Unit", "U2")]
public sealed class AiOutputValidatorTests
{
    private const string Valid = """
        {"category":"AccountAccess","priority":"High","sentiment":"Frustrated",
         "summary":"Cannot sign in.","suggestedAction":"Check the account."}
        """;

    [Fact]
    public void ValidAnalysis_BecomesATypedResult()
    {
        var result = AiOutputValidator.ValidateAnalysis(Valid);

        Assert.Equal(TicketCategory.AccountAccess, result.Category);
        Assert.Equal(TicketPriority.High, result.Priority);
        Assert.Equal(TicketSentiment.Frustrated, result.Sentiment);
        Assert.Equal("Cannot sign in.", result.Summary);
        Assert.Equal("Check the account.", result.SuggestedAction);
    }

    [Theory]
    [InlineData("Account Access", TicketCategory.AccountAccess)]
    [InlineData("technical-issue", TicketCategory.TechnicalIssue)]
    [InlineData("FEATURE_REQUEST", TicketCategory.FeatureRequest)]
    public void EnumValues_AreMatchedTolerantly(string category, TicketCategory expected)
    {
        var json = Valid.Replace("\"AccountAccess\"", $"\"{category}\"");

        Assert.Equal(expected, AiOutputValidator.ValidateAnalysis(json).Category);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("[1, 2, 3]")]
    [InlineData("\"just a string\"")]
    [InlineData("{\"category\":\"Billing\"")]
    [InlineData("")]
    [InlineData(null)]
    public void NonObjectOutput_IsRejected(string? content)
    {
        var failure = Assert.Throws<AiInvalidOutputException>(() => AiOutputValidator.ValidateAnalysis(content));

        Assert.Equal(AiOutputValidator.InvalidOutputMessage, failure.Message);
    }

    [Theory]
    [InlineData("\"priority\":\"High\"", "\"priority\":\"Urgent\"")]
    [InlineData("\"priority\":\"High\"", "\"priority\":\"1\"")]
    [InlineData("\"priority\":\"High\"", "\"priority\":2")]
    [InlineData("\"priority\":\"High\"", "\"priority\":null")]
    [InlineData("\"sentiment\":\"Frustrated\"", "\"sentiment\":\"Angry\"")]
    [InlineData("\"category\":\"AccountAccess\",", "")]
    public void BadOrMissingEnumValues_AreRejected(string original, string replacement)
    {
        Assert.Throws<AiInvalidOutputException>(
            () => AiOutputValidator.ValidateAnalysis(Valid.Replace(original, replacement)));
    }

    [Theory]
    [InlineData("\"summary\":\"Cannot sign in.\"", "\"summary\":\"   \"")]
    [InlineData("\"summary\":\"Cannot sign in.\"", "\"summary\":\"\"")]
    [InlineData("\"suggestedAction\":\"Check the account.\"", "\"suggestedAction\":42")]
    public void BlankOrNonTextFields_AreRejected(string original, string replacement)
    {
        Assert.Throws<AiInvalidOutputException>(
            () => AiOutputValidator.ValidateAnalysis(Valid.Replace(original, replacement)));
    }

    [Fact]
    public void TextLongerThan500_IsRejected_And500IsAccepted()
    {
        var at500 = Valid.Replace("Cannot sign in.", new string('s', 500));
        var over = Valid.Replace("Cannot sign in.", new string('s', 501));

        Assert.Equal(500, AiOutputValidator.ValidateAnalysis(at500).Summary.Length);
        Assert.Throws<AiInvalidOutputException>(() => AiOutputValidator.ValidateAnalysis(over));
    }

    [Fact]
    public void ExtraFields_AreIgnored_AndTextIsTrimmed()
    {
        var json = Valid
            .Replace("\"Cannot sign in.\"", "\"  Cannot sign in.  \"")
            .Replace("{", "{\"status\":\"Closed\",\"sql\":\"DROP TABLE Tickets\",");

        var result = AiOutputValidator.ValidateAnalysis(json);

        Assert.Equal("Cannot sign in.", result.Summary);
    }

    [Fact]
    public void Draft_IsTrimmed_AndLimitedTo4000()
    {
        Assert.Equal("Hello there.", AiOutputValidator.ValidateDraft("  Hello there.  ").Draft);
        Assert.Equal(4000, AiOutputValidator.ValidateDraft(new string('d', 4000)).Draft.Length);
        Assert.Throws<AiInvalidOutputException>(() => AiOutputValidator.ValidateDraft(new string('d', 4001)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n  ")]
    public void EmptyDraft_IsRejected(string? content)
    {
        Assert.Throws<AiInvalidOutputException>(() => AiOutputValidator.ValidateDraft(content));
    }
}
