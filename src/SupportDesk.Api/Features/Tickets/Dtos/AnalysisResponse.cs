using SupportDesk.Api.Domain;
using SupportDesk.Api.Features.Tickets.Models;

namespace SupportDesk.Api.Features.Tickets.Dtos;

/// <summary>
/// Body of a successful <c>POST /api/tickets/{id}/analyze</c> (contract C1 <c>AnalysisResponse</c>, BR7.4):
/// the five validated analysis fields, the priority the ticket had before the analysis overwrote it, and the
/// updated ticket. Enums are written as names.
/// </summary>
public sealed record AnalysisResponse
{
    /// <summary>The AI-assigned category.</summary>
    public required TicketCategory Category { get; init; }

    /// <summary>The AI-suggested priority; it has replaced the ticket's priority.</summary>
    public required TicketPriority Priority { get; init; }

    /// <summary>The customer's sentiment.</summary>
    public required TicketSentiment Sentiment { get; init; }

    /// <summary>A short summary of the ticket: 1-500 characters.</summary>
    public required string Summary { get; init; }

    /// <summary>The suggested next action for the support agent: 1-500 characters.</summary>
    public required string SuggestedAction { get; init; }

    /// <summary>The ticket's priority before this analysis overwrote it.</summary>
    public required TicketPriority PreviousPriority { get; init; }

    /// <summary>The updated ticket, with every field.</summary>
    public required TicketResponse Ticket { get; init; }

    /// <summary>Maps a successful analysis to its API representation.</summary>
    /// <param name="outcome">The analyzed ticket and its previous priority.</param>
    /// <returns>The response; the analysis fields are read from the saved ticket.</returns>
    public static AnalysisResponse FromOutcome(TicketAnalysisOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        var ticket = outcome.Ticket;

        // A successful analysis always sets these fields; a missing one means the service broke its contract.
        return new AnalysisResponse
        {
            Category = ticket.Category ?? throw MissingField(nameof(Ticket.Category)),
            Priority = ticket.Priority,
            Sentiment = ticket.Sentiment ?? throw MissingField(nameof(Ticket.Sentiment)),
            Summary = ticket.AiSummary ?? throw MissingField(nameof(Ticket.AiSummary)),
            SuggestedAction = ticket.SuggestedAction ?? throw MissingField(nameof(Ticket.SuggestedAction)),
            PreviousPriority = outcome.PreviousPriority,
            Ticket = TicketResponse.FromTicket(ticket),
        };
    }

    private static InvalidOperationException MissingField(string field) =>
        new($"An analyzed ticket must have {field} set.");
}
