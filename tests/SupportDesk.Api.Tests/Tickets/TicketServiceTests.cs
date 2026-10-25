using SupportDesk.Api.Domain;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Features.Tickets.Models;
using SupportDesk.Api.Tests.TestSupport;

namespace SupportDesk.Api.Tests.Tickets;

/// <summary>Ticket business rules over a real SQLite database (BR1-BR5, FR3.3).</summary>
[Trait("Unit", "U1")]
public sealed class TicketServiceTests : IDisposable
{
    private readonly ServiceHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public async Task Create_TrimsText_AndAppliesDefaults()
    {
        var ticket = await _harness.Tickets().CreateAsync(
            new CreateTicketInput("  Login broken  ", "  The login page rejects my password.  "),
            CancellationToken.None);

        var saved = await _harness.ReloadAsync(ticket.Id);
        Assert.NotNull(saved);
        Assert.Equal("Login broken", saved!.Title);
        Assert.Equal("The login page rejects my password.", saved.Description);
        Assert.Equal(TicketStatus.Open, saved.Status);
        Assert.Equal(TicketPriority.Medium, saved.Priority);
        Assert.Null(saved.Category);
        Assert.Null(saved.AiSummary);
        Assert.Equal(_harness.Clock.UtcNow, saved.CreatedAt);
        Assert.Equal(saved.CreatedAt, saved.UpdatedAt);
    }

    [Fact]
    public async Task Create_WithInvalidInput_SavesNothing()
    {
        await Assert.ThrowsAsync<ValidationFailedException>(() => _harness.Tickets().CreateAsync(
            new CreateTicketInput(" ", "short"),
            CancellationToken.None));

        var page = await _harness.Tickets().ListAsync(TicketListFilter.None, PageRequest.First, CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task List_IsNewestFirst_WithIdAsTieBreak()
    {
        var sameTime = _harness.Clock.UtcNow;
        var older = await _harness.SeedAsync("Older", createdAt: sameTime.AddHours(-1));
        var first = await _harness.SeedAsync("Same time A", createdAt: sameTime);
        var second = await _harness.SeedAsync("Same time B", createdAt: sameTime);

        var page = await _harness.Tickets().ListAsync(TicketListFilter.None, PageRequest.First, CancellationToken.None);

        Assert.Equal([second.Id, first.Id, older.Id], page.Items.Select(t => t.Id));
    }

    [Fact]
    public async Task List_CombinesFiltersWithAnd()
    {
        await _harness.SeedAsync("Open high", TicketStatus.Open, TicketPriority.High);
        await _harness.SeedAsync("Open low", TicketStatus.Open, TicketPriority.Low);
        await _harness.SeedAsync("Closed high", TicketStatus.Closed, TicketPriority.High);
        await _harness.SeedAsync("Billing", TicketStatus.Open, TicketPriority.Medium, TicketCategory.Billing);

        var openHigh = await _harness.Tickets().ListAsync(
            new TicketListFilter(TicketStatus.Open, TicketPriority.High),
            PageRequest.First,
            CancellationToken.None);
        var billing = await _harness.Tickets().ListAsync(
            new TicketListFilter(Category: TicketCategory.Billing),
            PageRequest.First,
            CancellationToken.None);

        Assert.Equal(["Open high"], openHigh.Items.Select(t => t.Title));
        Assert.Equal(["Billing"], billing.Items.Select(t => t.Title));
    }

    [Fact]
    public async Task List_ReturnsOnePage_WithTotals_AndPagesDoNotOverlap()
    {
        for (var i = 1; i <= 7; i++)
        {
            await _harness.SeedAsync($"Ticket {i}", createdAt: _harness.Clock.UtcNow.AddMinutes(i));
        }

        var service = _harness.Tickets();
        var page1 = await service.ListAsync(TicketListFilter.None, new PageRequest(1, 3), CancellationToken.None);
        var page3 = await service.ListAsync(TicketListFilter.None, new PageRequest(3, 3), CancellationToken.None);
        var past = await service.ListAsync(TicketListFilter.None, new PageRequest(4, 3), CancellationToken.None);

        Assert.Equal(["Ticket 7", "Ticket 6", "Ticket 5"], page1.Items.Select(t => t.Title));
        Assert.Equal(["Ticket 1"], page3.Items.Select(t => t.Title));
        Assert.Empty(past.Items);
        Assert.Equal(7, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
    }

    [Fact]
    public async Task List_UsesDefaultPaging_AndRejectsBadPaging()
    {
        var page = await _harness.Tickets().ListAsync(TicketListFilter.None, PageRequest.First, CancellationToken.None);

        Assert.Equal(PageRequest.DefaultPage, page.Page);
        Assert.Equal(PageRequest.DefaultPageSize, page.PageSize);
        Assert.Equal(0, page.TotalPages);
        await Assert.ThrowsAsync<ValidationFailedException>(() => _harness.Tickets().ListAsync(
            TicketListFilter.None,
            new PageRequest(0, PageRequest.MaxPageSize + 1),
            CancellationToken.None));
    }

    [Fact]
    public async Task Get_UnknownId_IsNotFound()
    {
        var failure = await Assert.ThrowsAsync<TicketNotFoundException>(
            () => _harness.Tickets().GetAsync(404, CancellationToken.None));

        Assert.Equal(404, failure.TicketId);
    }

    [Fact]
    public async Task Update_ChangesOnlyTitleDescriptionAndPriority()
    {
        var seeded = await _harness.SeedAsync("Original", TicketStatus.InProgress, category: TicketCategory.Billing);
        _harness.Clock.Advance(TimeSpan.FromMinutes(5));

        await _harness.Tickets().UpdateAsync(
            seeded.Id,
            new UpdateTicketInput("  New title ", "A new long enough description.", TicketPriority.Critical),
            CancellationToken.None);

        var saved = await _harness.ReloadAsync(seeded.Id);
        Assert.Equal("New title", saved!.Title);
        Assert.Equal(TicketPriority.Critical, saved.Priority);
        Assert.Equal(TicketStatus.InProgress, saved.Status);
        Assert.Equal(TicketCategory.Billing, saved.Category);
        Assert.Equal(_harness.Clock.UtcNow, saved.UpdatedAt);
    }

    [Fact]
    public async Task Update_Invalid_OrUnknown_ChangesNothing()
    {
        var seeded = await _harness.SeedAsync("Original");

        await Assert.ThrowsAsync<ValidationFailedException>(() => _harness.Tickets().UpdateAsync(
            seeded.Id,
            new UpdateTicketInput("ab", "short", null),
            CancellationToken.None));
        await Assert.ThrowsAsync<TicketNotFoundException>(() => _harness.Tickets().UpdateAsync(
            999,
            new UpdateTicketInput("Valid title", "A long enough description.", TicketPriority.Low),
            CancellationToken.None));

        Assert.Equal("Original", (await _harness.ReloadAsync(seeded.Id))!.Title);
    }

    [Theory]
    [InlineData(TicketStatus.Open, TicketStatus.Closed)]
    [InlineData(TicketStatus.Closed, TicketStatus.Open)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Resolved)]
    public async Task ChangeStatus_AllowsAnyStatusFromAnyStatus(TicketStatus from, TicketStatus to)
    {
        var seeded = await _harness.SeedAsync("Status test", from);
        _harness.Clock.Advance(TimeSpan.FromMinutes(1));

        var ticket = await _harness.Tickets().ChangeStatusAsync(seeded.Id, to, CancellationToken.None);

        Assert.Equal(to, ticket.Status);
        Assert.Equal(_harness.Clock.UtcNow, (await _harness.ReloadAsync(seeded.Id))!.UpdatedAt);
    }

    [Fact]
    public async Task ChangeStatus_Missing_OrUnknownTicket_Fails()
    {
        var seeded = await _harness.SeedAsync("Status test");

        await Assert.ThrowsAsync<ValidationFailedException>(
            () => _harness.Tickets().ChangeStatusAsync(seeded.Id, null, CancellationToken.None));
        await Assert.ThrowsAsync<TicketNotFoundException>(
            () => _harness.Tickets().ChangeStatusAsync(999, TicketStatus.Closed, CancellationToken.None));
    }
}
