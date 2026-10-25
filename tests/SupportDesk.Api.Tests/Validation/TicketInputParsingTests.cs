using SupportDesk.Api.Domain;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Features.Tickets.Models;
using SupportDesk.Api.Features.Tickets.Validation;

namespace SupportDesk.Api.Tests.Validation;

/// <summary>The id and enum parsing shared by the REST controller and the MCP tools (BR3.2, BR2.2, BR2.3, FR15.1).</summary>
[Trait("Unit", "U1")]
public sealed class TicketInputParsingTests
{
    [Theory]
    [InlineData("1", 1)]
    [InlineData("42", 42)]
    public void ParseId_AcceptsPositiveWholeNumbers(string text, int expected)
    {
        Assert.Equal(expected, TicketInputParsing.ParseId(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-3")]
    [InlineData("1.5")]
    [InlineData(" 7")]
    [InlineData("99999999999")]
    public void ParseId_RejectsAnythingElse_NamingTheIdField(string? text)
    {
        var failure = Assert.Throws<ValidationFailedException>(() => TicketInputParsing.ParseId(text));

        Assert.Equal(["Id must be a positive whole number."], failure.Errors["id"]);
    }

    [Fact]
    public void ValidateId_AcceptsOneOrMore_AndRejectsZeroOrNegative()
    {
        Assert.Equal(5, TicketInputParsing.ValidateId(5));
        Assert.Throws<ValidationFailedException>(() => TicketInputParsing.ValidateId(0));
        Assert.Throws<ValidationFailedException>(() => TicketInputParsing.ValidateId(-1));
    }

    [Theory]
    [InlineData("Open", TicketStatus.Open)]
    [InlineData("inprogress", TicketStatus.InProgress)]
    [InlineData("  CLOSED ", TicketStatus.Closed)]
    public void ParseOptionalEnum_MatchesNamesIgnoringCase(string text, TicketStatus expected)
    {
        var errors = new Dictionary<string, string[]>();

        Assert.Equal(expected, TicketInputParsing.ParseOptionalEnum<TicketStatus>(text, "status", errors));
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("Done")]
    [InlineData("Open,Closed")]
    public void ParseOptionalEnum_RejectsNumbersAndUnknownNames_ListingAllowedValues(string text)
    {
        var errors = new Dictionary<string, string[]>();

        Assert.Null(TicketInputParsing.ParseOptionalEnum<TicketStatus>(text, "status", errors));
        Assert.Equal(["Status is not valid. Allowed values: Open, InProgress, Resolved, Closed."], errors["status"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseOptionalEnum_TreatsEmptyAsNotGiven(string? text)
    {
        var errors = new Dictionary<string, string[]>();

        Assert.Null(TicketInputParsing.ParseOptionalEnum<TicketPriority>(text, "priority", errors));
        Assert.Empty(errors);
    }

    [Fact]
    public void ParseEnumOrThrow_ReturnsTheValue_OrThrowsForAnInvalidOne()
    {
        Assert.Equal(TicketPriority.High, TicketInputParsing.ParseEnumOrThrow<TicketPriority>("high", "priority"));
        Assert.Null(TicketInputParsing.ParseEnumOrThrow<TicketPriority>(null, "priority"));
        Assert.Throws<ValidationFailedException>(() => TicketInputParsing.ParseEnumOrThrow<TicketPriority>("Urgent", "priority"));
    }

    [Fact]
    public void ParseListFilter_ParsesAllFilters()
    {
        var filter = TicketInputParsing.ParseListFilter("open", "HIGH", "Billing", PageRequest.First);

        Assert.Equal(new TicketListFilter(TicketStatus.Open, TicketPriority.High, TicketCategory.Billing), filter);
    }

    [Fact]
    public void ParseListFilter_ReportsFilterAndPagingErrorsTogether()
    {
        var failure = Assert.Throws<ValidationFailedException>(() =>
            TicketInputParsing.ParseListFilter("Pending", null, "Food", new PageRequest(0, 500)));

        Assert.Equal(["category", "page", "pageSize", "status"], failure.Errors.Keys.Order());
    }

    [Fact]
    public void ParseListFilter_UsesTheCallersPageSizeCap()
    {
        var failure = Assert.Throws<ValidationFailedException>(() =>
            TicketInputParsing.ParseListFilter(null, null, null, new PageRequest(1, 60), maxPageSize: 50));

        Assert.Equal(["Page size must be between 1 and 50."], failure.Errors["pageSize"]);
    }
}
