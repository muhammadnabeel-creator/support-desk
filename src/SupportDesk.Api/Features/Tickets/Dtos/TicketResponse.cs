using SupportDesk.Api.Domain;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Features.Tickets.Dtos;

/// <summary>
/// A ticket as returned by the API, with every field (contract C1 <c>Ticket</c> schema, BR3.3).
/// Enums are written as names; timestamps are ISO 8601 UTC ending in <c>Z</c>.
/// Kept separate from the <see cref="Ticket"/> entity so the entity is never bound from or exposed to HTTP directly.
/// </summary>
public sealed record TicketResponse
{
    /// <summary>The ticket id (a positive whole number).</summary>
    /// <example>1</example>
    public required int Id { get; init; }

    /// <summary>Short summary of the request (stored trimmed).</summary>
    public required string Title { get; init; }

    /// <summary>Full description of the request (stored trimmed).</summary>
    public required string Description { get; init; }

    /// <summary>Lifecycle status.</summary>
    public required TicketStatus Status { get; init; }

    /// <summary>Priority; Medium for a new ticket.</summary>
    public required TicketPriority Priority { get; init; }

    /// <summary>Category from AI analysis; null until the ticket is analyzed.</summary>
    public TicketCategory? Category { get; init; }

    /// <summary>Sentiment from AI analysis; null until the ticket is analyzed.</summary>
    public TicketSentiment? Sentiment { get; init; }

    /// <summary>Short AI summary; null until the ticket is analyzed.</summary>
    public string? AiSummary { get; init; }

    /// <summary>AI-suggested next action; null until the ticket is analyzed.</summary>
    public string? SuggestedAction { get; init; }

    /// <summary>UTC creation time.</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>UTC time of the last change.</summary>
    public required DateTime UpdatedAt { get; init; }

    /// <summary>Maps a ticket entity to its API representation.</summary>
    /// <param name="ticket">The ticket to map.</param>
    /// <returns>The response with every field copied and timestamps marked as UTC.</returns>
    public static TicketResponse FromTicket(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        return new TicketResponse
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            Priority = ticket.Priority,
            Category = ticket.Category,
            Sentiment = ticket.Sentiment,
            AiSummary = ticket.AiSummary,
            SuggestedAction = ticket.SuggestedAction,

            // Values are already UTC; normalising here guarantees the JSON always ends in "Z".
            CreatedAt = UtcDateTimeConverter.ToUtc(ticket.CreatedAt),
            UpdatedAt = UtcDateTimeConverter.ToUtc(ticket.UpdatedAt),
        };
    }
}
