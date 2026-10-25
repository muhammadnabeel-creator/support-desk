using SupportDesk.Api.Domain;

namespace SupportDesk.Api.Features.Tickets.Dtos;

/// <summary>
/// Body of <c>PUT /api/tickets/{id}</c> (contract C1). All three fields are required together (BR4.1).
/// Status and AI fields are not part of this request; if a client sends them they are ignored (BR4.2).
/// Properties are nullable so a missing priority is a 400 instead of silently becoming <c>Low</c>.
/// </summary>
public sealed record UpdateTicketRequest
{
    /// <summary>The new title: 3-200 characters after trimming.</summary>
    /// <example>Cannot log in after password reset</example>
    public string? Title { get; init; }

    /// <summary>The new description: 10-4000 characters after trimming.</summary>
    /// <example>The account is still locked two hours after the reset.</example>
    public string? Description { get; init; }

    /// <summary>The new priority, by name (case-insensitive): Low, Medium, High or Critical.</summary>
    /// <example>High</example>
    public TicketPriority? Priority { get; init; }
}
