using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Domain;

namespace SupportDesk.Api.Infrastructure;

/// <summary>
/// EF Core context for the SQLite database. The schema is created and changed only through
/// migrations (<c>dotnet ef database update</c>); the app never migrates at startup.
/// </summary>
public class SupportDeskDbContext : DbContext
{
    /// <summary>Maximum stored length of an enum name column.</summary>
    public const int EnumNameMaxLength = 32;

    /// <summary>Creates the context with options configured through dependency injection.</summary>
    /// <param name="options">The context options (SQLite provider and connection string).</param>
    public SupportDeskDbContext(DbContextOptions<SupportDeskDbContext> options)
        : base(options)
    {
    }

    /// <summary>All support tickets.</summary>
    public DbSet<Ticket> Tickets => Set<Ticket>();

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Every DateTime (and DateTime?) is written as UTC and read back with DateTimeKind.Utc.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var ticket = modelBuilder.Entity<Ticket>();

        ticket.HasKey(t => t.Id);

        ticket.Property(t => t.Title).IsRequired().HasMaxLength(Ticket.TitleMaxLength);
        ticket.Property(t => t.Description).IsRequired().HasMaxLength(Ticket.DescriptionMaxLength);
        ticket.Property(t => t.AiSummary).HasMaxLength(Ticket.AiSummaryMaxLength);
        ticket.Property(t => t.SuggestedAction).HasMaxLength(Ticket.SuggestedActionMaxLength);
        ticket.Property(t => t.DraftReply).HasMaxLength(Ticket.DraftReplyMaxLength);

        // Enums are stored by name, never by number (entity constraint, contract C3).
        ticket.Property(t => t.Status).HasConversion<string>().HasMaxLength(EnumNameMaxLength);
        ticket.Property(t => t.Priority).HasConversion<string>().HasMaxLength(EnumNameMaxLength);
        ticket.Property(t => t.Category).HasConversion<string>().HasMaxLength(EnumNameMaxLength);
        ticket.Property(t => t.Sentiment).HasConversion<string>().HasMaxLength(EnumNameMaxLength);

        // Lists are ordered newest first (BR2.1).
        ticket.HasIndex(t => t.CreatedAt);
    }
}
