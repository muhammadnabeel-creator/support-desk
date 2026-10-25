using SupportDesk.Api.Domain;

namespace SupportDesk.Api.Features.Ai.Models;

/// <summary>
/// The saved analysis of a ticket, given to the draft prompt so the reply fits the ticket (BR7.3).
/// The Tickets service passes it only when the ticket has been analyzed (its category is set).
/// The optional values are left out of the prompt when they are missing.
/// </summary>
/// <param name="Category">The saved category.</param>
/// <param name="Sentiment">The saved sentiment, if any.</param>
/// <param name="Summary">The saved AI summary, if any.</param>
/// <param name="SuggestedAction">The saved suggested action, if any.</param>
public sealed record AiDraftContext(
    TicketCategory Category,
    TicketSentiment? Sentiment,
    string? Summary,
    string? SuggestedAction);
