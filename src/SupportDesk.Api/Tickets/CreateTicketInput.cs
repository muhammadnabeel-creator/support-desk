namespace SupportDesk.Api.Tickets;

/// <summary>
/// What the ticket service needs to create a ticket. Values are raw (untrimmed, possibly missing);
/// the service trims and validates them (BR1.1-BR1.3). HTTP request DTOs map onto this record.
/// </summary>
/// <param name="Title">The requested title.</param>
/// <param name="Description">The requested description.</param>
public sealed record CreateTicketInput(string? Title, string? Description);
