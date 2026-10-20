using System.ComponentModel.DataAnnotations;

namespace SupportDesk.Api.Tickets.Dtos;

/// <summary>
/// Body of <c>POST /api/tickets</c> (contract C1). Only these two fields are read; any other field in the
/// request is ignored, so status, priority and AI fields always start at their defaults (BR1.4).
/// Properties are nullable so a missing field is reported as missing; trimming and length checks
/// are done by the ticket service (BR1.1, BR1.2).
/// </summary>
public sealed record CreateTicketRequest
{
    /// <summary>Short summary of the request: 3-200 characters after trimming.</summary>
    /// <example>Cannot log in after password reset</example>
    [Required(ErrorMessage = "Title is required.")]
    public string? Title { get; init; }

    /// <summary>Full description of the request: 10-4000 characters after trimming.</summary>
    /// <example>I reset my password this morning and now the login page says my account is locked.</example>
    [Required(ErrorMessage = "Description is required.")]
    public string? Description { get; init; }
}
