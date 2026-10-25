namespace SupportDesk.Api.Features.Tickets.Models;

/// <summary>
/// A reply draft generated for a ticket (FR-07). It is returned for a human to review and edit; it is not stored
/// and never sent.
/// </summary>
/// <param name="TicketId">The ticket the draft was written for.</param>
/// <param name="Draft">The validated draft text, 1-4000 characters.</param>
/// <param name="GeneratedAt">UTC time the draft was generated.</param>
public sealed record TicketDraft(int TicketId, string Draft, DateTime GeneratedAt);
