using SupportDesk.Api.Ai;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Errors;

namespace SupportDesk.Api.Tickets;

/// <summary>
/// Ticket business rules (BR1.1-BR5.2, BR7.1-BR9.4) over the database. Only <see cref="AnalyzeAsync"/> and
/// <see cref="DraftReplyAsync"/> use the AI service; the other operations never touch it (BR6.5, NFR1.2), so
/// they keep working when the AI provider is unavailable. Failures are raised as
/// <see cref="ValidationFailedException"/> (400) or <see cref="TicketNotFoundException"/> (404); the two AI
/// operations can also fail with the AI exceptions listed on <see cref="IAiTicketService"/>.
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

    /// <summary>
    /// Analyzes a ticket with the AI and saves the validated result (WF7). Works in any status (BR7.1) and never
    /// changes the status (BR8.5). Overwrites category, sentiment, summary, suggested action and priority, replacing
    /// any earlier analysis (BR7.4, BR7.5), and refreshes <c>UpdatedAt</c>. On any AI failure nothing is saved (BR7.6).
    /// </summary>
    /// <param name="id">The ticket id.</param>
    /// <param name="cancellationToken">Cancels the database work and the AI call.</param>
    /// <returns>The saved ticket and the priority it had before the analysis.</returns>
    /// <exception cref="TicketNotFoundException">No ticket has this id; the AI is not called (BR7.2).</exception>
    /// <exception cref="AiNotConfiguredException">No API key is configured.</exception>
    /// <exception cref="AiTimeoutException">The AI provider did not answer in time.</exception>
    /// <exception cref="AiProviderException">The AI provider failed or could not be reached.</exception>
    /// <exception cref="AiInvalidOutputException">The AI output failed validation.</exception>
    Task<TicketAnalysisOutcome> AnalyzeAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Asks the AI for a reply draft and stores it on the ticket for human review (WF8). The draft is never sent
    /// (BR9.4). Works in any status and never changes it (BR7.1, BR8.5). The saved analysis is given to the AI only
    /// when the ticket has been analyzed (<c>Category</c> is set). Replaces any earlier draft and sets
    /// <c>DraftReplyGeneratedAt</c> and <c>UpdatedAt</c> to the same time (BR9.2). On any AI failure nothing is saved (BR7.6).
    /// </summary>
    /// <param name="id">The ticket id.</param>
    /// <param name="cancellationToken">Cancels the database work and the AI call.</param>
    /// <returns>The saved ticket, holding the new draft.</returns>
    /// <exception cref="TicketNotFoundException">No ticket has this id; the AI is not called (BR7.2).</exception>
    /// <exception cref="AiNotConfiguredException">No API key is configured.</exception>
    /// <exception cref="AiTimeoutException">The AI provider did not answer in time.</exception>
    /// <exception cref="AiProviderException">The AI provider failed or could not be reached.</exception>
    /// <exception cref="AiInvalidOutputException">The AI output failed validation.</exception>
    Task<Ticket> DraftReplyAsync(int id, CancellationToken cancellationToken);
}
