using SupportDesk.Api.Domain;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Features.Tickets.Models;
using SupportDesk.Api.Features.Tickets.Validation;

namespace SupportDesk.Api.Tests.Validation;

/// <summary>The FluentValidation rules for ticket input (BR1.1, BR1.2, BR4.1, BR5.2, FR3.3).</summary>
[Trait("Unit", "U1")]
public sealed class TicketValidatorsTests
{
    private static IReadOnlyDictionary<string, string[]> ErrorsOf(Action validate) =>
        Assert.Throws<ValidationFailedException>(validate).Errors;

    [Fact]
    public void Create_AcceptsTrimmedTextAtTheLimits()
    {
        var validator = new CreateTicketInputValidator();

        validator.ValidateOrThrow(new CreateTicketInput("  abc  ", new string('d', 10)));
        validator.ValidateOrThrow(new CreateTicketInput(new string('t', 200), new string('d', 4000)));
    }

    [Theory]
    [InlineData(null, "Title is required.")]
    [InlineData("", "Title is required.")]
    [InlineData("   ", "Title is required.")]
    [InlineData("ab", "Title must be between 3 and 200 characters after trimming.")]
    [InlineData("  ab  ", "Title must be between 3 and 200 characters after trimming.")]
    public void Create_RejectsBadTitles(string? title, string message)
    {
        var errors = ErrorsOf(() => new CreateTicketInputValidator()
            .ValidateOrThrow(new CreateTicketInput(title, "A long enough description.")));

        Assert.Equal([message], errors["title"]);
        Assert.False(errors.ContainsKey("description"));
    }

    [Fact]
    public void Create_RejectsOverlongTitleAndDescription()
    {
        var errors = ErrorsOf(() => new CreateTicketInputValidator()
            .ValidateOrThrow(new CreateTicketInput(new string('t', 201), new string('d', 4001))));

        Assert.Equal(["Title must be between 3 and 200 characters after trimming."], errors["title"]);
        Assert.Equal(["Description must be between 10 and 4000 characters after trimming."], errors["description"]);
    }

    [Fact]
    public void Create_ReportsEveryFailingField_WithOneMessageEach()
    {
        var errors = ErrorsOf(() => new CreateTicketInputValidator()
            .ValidateOrThrow(new CreateTicketInput("  ", "short")));

        Assert.Equal(2, errors.Count);
        Assert.Single(errors["title"]);
        Assert.Single(errors["description"]);
    }

    [Fact]
    public void Update_RequiresAValidPriority()
    {
        var validator = new UpdateTicketInputValidator();

        var missing = ErrorsOf(() => validator.ValidateOrThrow(
            new UpdateTicketInput("Valid title", "A long enough description.", null)));
        var undefined = ErrorsOf(() => validator.ValidateOrThrow(
            new UpdateTicketInput("Valid title", "A long enough description.", (TicketPriority)99)));

        Assert.Equal(["Priority is required. Allowed values: Low, Medium, High, Critical."], missing["priority"]);
        Assert.Equal(["Priority must be one of: Low, Medium, High, Critical."], undefined["priority"]);
        validator.ValidateOrThrow(new UpdateTicketInput("Valid title", "A long enough description.", TicketPriority.Low));
    }

    [Fact]
    public void Update_ReportsAllThreeFieldsWhenEmpty()
    {
        var errors = ErrorsOf(() => new UpdateTicketInputValidator()
            .ValidateOrThrow(new UpdateTicketInput(null, null, null)));

        Assert.Equal(["description", "priority", "title"], errors.Keys.Order());
    }

    [Fact]
    public void ChangeStatus_RequiresADefinedStatus()
    {
        var validator = new ChangeStatusInputValidator();

        Assert.Equal(
            ["Status is required. Allowed values: Open, InProgress, Resolved, Closed."],
            ErrorsOf(() => validator.ValidateOrThrow(new ChangeStatusInput(null)))["status"]);
        Assert.Equal(
            ["Status must be one of: Open, InProgress, Resolved, Closed."],
            ErrorsOf(() => validator.ValidateOrThrow(new ChangeStatusInput((TicketStatus)42)))["status"]);
        validator.ValidateOrThrow(new ChangeStatusInput(TicketStatus.Closed));
    }

    [Fact]
    public void ListFilter_AllowsEmptyFilters_ButRejectsUndefinedValues()
    {
        var validator = new TicketListFilterValidator();

        validator.ValidateOrThrow(TicketListFilter.None);
        var errors = ErrorsOf(() => validator.ValidateOrThrow(
            new TicketListFilter((TicketStatus)9, (TicketPriority)9, (TicketCategory)9)));

        Assert.Equal(["category", "priority", "status"], errors.Keys.Order());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(1, 1)]
    [InlineData(1000, 200)]
    public void Paging_AcceptsDefaultsAndRange(int? page, int? pageSize)
    {
        new PageRequestValidator().ValidateOrThrow(new PageRequest(page, pageSize));
    }

    [Fact]
    public void Paging_RejectsOutOfRangeValues_AndHonoursASmallerCap()
    {
        var errors = ErrorsOf(() => new PageRequestValidator().ValidateOrThrow(new PageRequest(0, 201)));
        var mcp = ErrorsOf(() => new PageRequestValidator(50).ValidateOrThrow(new PageRequest(1, 51)));

        Assert.Equal(["Page must be 1 or greater."], errors["page"]);
        Assert.Equal(["Page size must be between 1 and 200."], errors["pageSize"]);
        Assert.Equal(["Page size must be between 1 and 50."], mcp["pageSize"]);
    }
}
