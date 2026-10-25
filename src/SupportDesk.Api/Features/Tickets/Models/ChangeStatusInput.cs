using SupportDesk.Api.Domain;

namespace SupportDesk.Api.Features.Tickets.Models;

/// <summary>The input of a status change (BR5.1, BR5.2).</summary>
/// <param name="Status">The new status; <c>null</c> when it was missing from the request.</param>
public sealed record ChangeStatusInput(TicketStatus? Status);
