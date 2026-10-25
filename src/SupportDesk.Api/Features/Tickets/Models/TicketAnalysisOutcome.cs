using SupportDesk.Api.Domain;

namespace SupportDesk.Api.Features.Tickets.Models;

/// <summary>
/// The result of a successful analysis (BR7.4): the saved ticket, now holding the new AI fields and the
/// AI-suggested priority, together with the priority it had before the analysis overwrote it.
/// </summary>
/// <param name="Ticket">The analyzed and saved ticket.</param>
/// <param name="PreviousPriority">The ticket's priority before the analysis.</param>
public sealed record TicketAnalysisOutcome(Ticket Ticket, TicketPriority PreviousPriority);
