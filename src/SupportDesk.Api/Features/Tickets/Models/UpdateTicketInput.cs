using SupportDesk.Api.Domain;

namespace SupportDesk.Api.Features.Tickets.Models;

/// <summary>
/// What the ticket service needs to update a ticket. All three values are required together (BR4.1);
/// status and AI fields are deliberately absent so an update can never change them (BR4.2).
/// </summary>
/// <param name="Title">The new title (raw; trimmed and validated by the service).</param>
/// <param name="Description">The new description (raw; trimmed and validated by the service).</param>
/// <param name="Priority">The new priority; null means it was missing from the request.</param>
public sealed record UpdateTicketInput(string? Title, string? Description, TicketPriority? Priority);
