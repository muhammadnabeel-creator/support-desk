using SupportDesk.Api.Domain;

namespace SupportDesk.Api.Tickets;

/// <summary>
/// Optional list filters, combined with AND (BR2.2). A null value means "do not filter on this field".
/// Parsing query-string text into these enums happens at the HTTP boundary (BR2.3).
/// </summary>
/// <param name="Status">Keep only tickets with this status.</param>
/// <param name="Priority">Keep only tickets with this priority.</param>
/// <param name="Category">Keep only tickets with this AI-assigned category.</param>
public sealed record TicketListFilter(
    TicketStatus? Status = null,
    TicketPriority? Priority = null,
    TicketCategory? Category = null)
{
    /// <summary>A filter that matches every ticket.</summary>
    public static TicketListFilter None { get; } = new();
}
