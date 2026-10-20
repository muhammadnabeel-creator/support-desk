using System.ComponentModel.DataAnnotations;
using SupportDesk.Api.Domain;

namespace SupportDesk.Api.Tickets.Dtos;

/// <summary>
/// Body of <c>PATCH /api/tickets/{id}/status</c> (contract C1). Any status may follow any status (BR5.1).
/// The property is nullable so a missing status is a 400 instead of silently becoming <c>Open</c> (BR5.2).
/// </summary>
public sealed record UpdateStatusRequest
{
    /// <summary>The new status, by name (case-insensitive): Open, InProgress, Resolved or Closed.</summary>
    /// <example>Resolved</example>
    [Required(ErrorMessage = "Status is required. Allowed values: Open, InProgress, Resolved, Closed.")]
    public TicketStatus? Status { get; init; }
}
