using SupportDesk.Api.Domain;
using SupportDesk.Api.Errors;

namespace SupportDesk.Api.Tickets;

/// <summary>
/// Ticket business rules (BR1.1-BR5.2) over the database. Has no AI dependency (BR6.5), so every
/// operation here keeps working when the AI provider is unavailable. Failures are raised as
/// <see cref="ValidationFailedException"/> (400) or <see cref="TicketNotFoundException"/> (404).
/// </summary>
public interface ITicketService
{
    /// <summary>
    /// Creates a ticket from trimmed, validated text with the default status, priority and timestamps (BR1.4).
    /// </summary>
    /// <param name="input">The raw title and description.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>The saved ticket, including its new id.</returns>
    /// <exception cref="ValidationFailedException">Title or description is missing or out of range.</exception>
    Task<Ticket> CreateAsync(CreateTicketInput input, CancellationToken cancellationToken);

    /// <summary>
    /// Lists tickets matching every present filter, newest <c>CreatedAt</c> first, ties by id descending (BR2.1, BR2.2).
    /// </summary>
    /// <param name="filter">The optional filters.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>The matching tickets; empty when nothing matches (BR2.4).</returns>
    /// <exception cref="ValidationFailedException">A filter holds a value that is not a defined enum member.</exception>
    Task<IReadOnlyList<Ticket>> ListAsync(TicketListFilter filter, CancellationToken cancellationToken);

    /// <summary>Gets one ticket with every field (BR3.3).</summary>
    /// <param name="id">The ticket id.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>The ticket.</returns>
    /// <exception cref="TicketNotFoundException">No ticket has this id (BR3.1).</exception>
    Task<Ticket> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces title, description and priority; never touches status or AI fields (BR4.1, BR4.2).
    /// Refreshes <c>UpdatedAt</c> (BR4.3).
    /// </summary>
    /// <param name="id">The ticket id.</param>
    /// <param name="input">The new values; all three are required.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>The updated ticket.</returns>
    /// <exception cref="ValidationFailedException">A value is missing or invalid; nothing is changed.</exception>
    /// <exception cref="TicketNotFoundException">No ticket has this id.</exception>
    Task<Ticket> UpdateAsync(int id, UpdateTicketInput input, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the status; any status may follow any status (BR5.1). Refreshes <c>UpdatedAt</c> (BR4.3).
    /// </summary>
    /// <param name="id">The ticket id.</param>
    /// <param name="status">The new status; null means it was missing from the request.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>The updated ticket.</returns>
    /// <exception cref="ValidationFailedException">The status is missing or unknown (BR5.2); nothing is changed.</exception>
    /// <exception cref="TicketNotFoundException">No ticket has this id.</exception>
    Task<Ticket> ChangeStatusAsync(int id, TicketStatus? status, CancellationToken cancellationToken);
}
