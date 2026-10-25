using SupportDesk.Api.Domain;
using SupportDesk.Api.Features.Dashboard;
using SupportDesk.Api.Features.Tickets;

namespace SupportDesk.Api.Tests.TestSupport;

/// <summary>
/// The ticket and dashboard services wired to a private SQLite in-memory database, a fake clock, the AI fake and
/// a controllable AI budget: everything a service test needs, with no web host.
/// </summary>
public sealed class ServiceHarness : IDisposable
{
    public ServiceHarness()
    {
        Database = new SqliteInMemoryDatabase();
        Clock = new FakeClock();
        Ai = new FakeAiTicketService();
        Limiter = new FakeAiCallLimiter();
    }

    public SqliteInMemoryDatabase Database { get; }

    public FakeClock Clock { get; }

    public FakeAiTicketService Ai { get; }

    public FakeAiCallLimiter Limiter { get; }

    /// <summary>A ticket service on a fresh context (a new unit of work, like one HTTP request).</summary>
    public TicketService Tickets() => new(Database.CreateContext(), Clock, Ai, Limiter);

    /// <summary>A dashboard service on a fresh context.</summary>
    public DashboardService Dashboard() => new(Database.CreateContext());

    /// <summary>Saves a ticket directly, for arranging data that the service API cannot produce.</summary>
    public async Task<Ticket> SeedAsync(
        string title,
        TicketStatus status = TicketStatus.Open,
        TicketPriority priority = TicketPriority.Medium,
        TicketCategory? category = null,
        DateTime? createdAt = null)
    {
        await using var context = Database.CreateContext();
        var when = createdAt ?? Clock.UtcNow;
        var ticket = new Ticket
        {
            Title = title,
            Description = "A seeded ticket with a long enough description.",
            Status = status,
            Priority = priority,
            Category = category,
            CreatedAt = when,
            UpdatedAt = when,
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();
        return ticket;
    }

    /// <summary>Reads a ticket back with a fresh context, to see exactly what was saved.</summary>
    public async Task<Ticket?> ReloadAsync(int id)
    {
        await using var context = Database.CreateContext();
        return await context.Tickets.FindAsync(id);
    }

    public void Dispose() => Database.Dispose();
}
