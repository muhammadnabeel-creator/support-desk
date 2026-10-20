using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Tests.TestSupport;

namespace SupportDesk.Api.Tests.Infrastructure;

/// <summary>
/// Persistence of <see cref="Ticket"/> through <c>SupportDeskDbContext</c> on real SQLite (in-memory).
/// </summary>
[Trait("Unit", "U1")]
public sealed class TicketPersistenceTests : IDisposable
{
    private static DateTime BaseTime => new(2026, 10, 20, 9, 30, 15, DateTimeKind.Utc);

    private readonly SqliteInMemoryDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task SaveAndReload_KeepsEveryField()
    {
        var original = new Ticket
        {
            Title = "Cannot log in",
            Description = "The login page rejects my password since this morning.",
            Status = TicketStatus.InProgress,
            Priority = TicketPriority.High,
            Category = TicketCategory.AccountAccess,
            Sentiment = TicketSentiment.Frustrated,
            AiSummary = "User locked out after password change.",
            SuggestedAction = "Reset the password and unlock the account.",
            DraftReply = "Hello, we have unlocked your account.",
            DraftReplyGeneratedAt = BaseTime.AddMinutes(5),
            CreatedAt = BaseTime,
            UpdatedAt = BaseTime.AddMinutes(10),
        };
        var id = await SaveAsync(original);

        await using var context = _database.CreateContext();
        var reloaded = await context.Tickets.AsNoTracking().SingleAsync(t => t.Id == id);

        Assert.True(reloaded.Id > 0);
        Assert.Equal(original.Title, reloaded.Title);
        Assert.Equal(original.Description, reloaded.Description);
        Assert.Equal(original.Status, reloaded.Status);
        Assert.Equal(original.Priority, reloaded.Priority);
        Assert.Equal(original.Category, reloaded.Category);
        Assert.Equal(original.Sentiment, reloaded.Sentiment);
        Assert.Equal(original.AiSummary, reloaded.AiSummary);
        Assert.Equal(original.SuggestedAction, reloaded.SuggestedAction);
        Assert.Equal(original.DraftReply, reloaded.DraftReply);
        Assert.Equal(original.DraftReplyGeneratedAt, reloaded.DraftReplyGeneratedAt);
        Assert.Equal(original.CreatedAt, reloaded.CreatedAt);
        Assert.Equal(original.UpdatedAt, reloaded.UpdatedAt);
    }

    [Fact]
    public async Task NewTicket_UsesDefaults_AndStoresAiFieldsAsNull()
    {
        var id = await SaveAsync(NewTicket("Printer jams", BaseTime));

        await using var context = _database.CreateContext();
        var reloaded = await context.Tickets.AsNoTracking().SingleAsync(t => t.Id == id);

        Assert.Equal(TicketStatus.Open, reloaded.Status);
        Assert.Equal(TicketPriority.Medium, reloaded.Priority);
        Assert.Null(reloaded.Category);
        Assert.Null(reloaded.Sentiment);
        Assert.Null(reloaded.AiSummary);
        Assert.Null(reloaded.SuggestedAction);
        Assert.Null(reloaded.DraftReply);
        Assert.Null(reloaded.DraftReplyGeneratedAt);
    }

    [Fact]
    public async Task Enums_AreStoredAsText()
    {
        var ticket = NewTicket("Refund request", BaseTime);
        ticket.Status = TicketStatus.InProgress;
        ticket.Priority = TicketPriority.Critical;
        ticket.Category = TicketCategory.Billing;
        ticket.Sentiment = TicketSentiment.Negative;
        var id = await SaveAsync(ticket);

        using var command = _database.Connection.CreateCommand();
        command.CommandText =
            "SELECT Status, Priority, Category, Sentiment, typeof(Status) FROM Tickets WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("InProgress", reader.GetString(0));
        Assert.Equal("Critical", reader.GetString(1));
        Assert.Equal("Billing", reader.GetString(2));
        Assert.Equal("Negative", reader.GetString(3));
        Assert.Equal("text", reader.GetString(4));
    }

    [Fact]
    public async Task Timestamps_ComeBackWithUtcKind()
    {
        var ticket = NewTicket("Slow dashboard", BaseTime);
        ticket.DraftReply = "Thanks, we are looking into it.";
        ticket.DraftReplyGeneratedAt = BaseTime.AddHours(1);
        var id = await SaveAsync(ticket);

        await using var context = _database.CreateContext();
        var reloaded = await context.Tickets.AsNoTracking().SingleAsync(t => t.Id == id);

        Assert.Equal(DateTimeKind.Utc, reloaded.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, reloaded.UpdatedAt.Kind);
        Assert.NotNull(reloaded.DraftReplyGeneratedAt);
        Assert.Equal(DateTimeKind.Utc, reloaded.DraftReplyGeneratedAt!.Value.Kind);
        Assert.Equal(BaseTime, reloaded.CreatedAt);
    }

    [Fact]
    public async Task LocalTimestamp_IsConvertedToUtcBeforeSaving()
    {
        var localTime = BaseTime.ToLocalTime();
        var id = await SaveAsync(NewTicket("Local clock", localTime));

        await using var context = _database.CreateContext();
        var reloaded = await context.Tickets.AsNoTracking().SingleAsync(t => t.Id == id);

        Assert.Equal(DateTimeKind.Utc, reloaded.CreatedAt.Kind);
        Assert.Equal(BaseTime, reloaded.CreatedAt);
    }

    [Fact]
    public async Task OrderingByCreatedAtDescendingThenIdDescending_WorksInSqlite()
    {
        var oldestId = await SaveAsync(NewTicket("Oldest", BaseTime));
        var tiedFirstId = await SaveAsync(NewTicket("Tied first", BaseTime.AddMinutes(1)));
        var tiedSecondId = await SaveAsync(NewTicket("Tied second", BaseTime.AddMinutes(1)));
        var newestId = await SaveAsync(NewTicket("Newest", BaseTime.AddDays(1)));

        await using var context = _database.CreateContext();
        var orderedIds = await context.Tickets
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Select(t => t.Id)
            .ToListAsync();

        Assert.Equal(new[] { newestId, tiedSecondId, tiedFirstId, oldestId }, orderedIds);
    }

    [Fact]
    public async Task MissingDescription_IsRejectedByTheDatabase()
    {
        var ticket = NewTicket("No description", BaseTime);
        ticket.Description = null!;

        await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(ticket));
    }

    [Fact]
    public void Model_DeclaresColumnLimitsAndCreatedAtIndex()
    {
        using var context = _database.CreateContext();
        var entity = context.Model.FindEntityType(typeof(Ticket));

        Assert.NotNull(entity);
        Assert.Equal(200, entity!.FindProperty(nameof(Ticket.Title))!.GetMaxLength());
        Assert.Equal(4000, entity.FindProperty(nameof(Ticket.Description))!.GetMaxLength());
        Assert.Equal(500, entity.FindProperty(nameof(Ticket.AiSummary))!.GetMaxLength());
        Assert.Equal(500, entity.FindProperty(nameof(Ticket.SuggestedAction))!.GetMaxLength());
        Assert.Equal(4000, entity.FindProperty(nameof(Ticket.DraftReply))!.GetMaxLength());
        Assert.False(entity.FindProperty(nameof(Ticket.Title))!.IsNullable);
        Assert.False(entity.FindProperty(nameof(Ticket.Description))!.IsNullable);
        Assert.Contains(
            entity.GetIndexes(),
            index => index.Properties.Count == 1 && index.Properties[0].Name == nameof(Ticket.CreatedAt));
    }

    private static Ticket NewTicket(string title, DateTime createdAt) => new()
    {
        Title = title,
        Description = "A description that is long enough.",
        CreatedAt = createdAt,
        UpdatedAt = createdAt,
    };

    private async Task<int> SaveAsync(Ticket ticket)
    {
        await using var context = _database.CreateContext();
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();
        return ticket.Id;
    }
}
