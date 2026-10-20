namespace SupportDesk.Api.Errors;

/// <summary>
/// Raised when an operation names a ticket id that does not exist (BR3.1).
/// The global exception handler maps it to 404 ProblemDetails naming the id.
/// </summary>
public class TicketNotFoundException : Exception
{
    /// <summary>Creates the failure for the given ticket id.</summary>
    /// <param name="ticketId">The id that was requested and not found.</param>
    public TicketNotFoundException(int ticketId)
        : base($"Ticket {ticketId} was not found.")
    {
        TicketId = ticketId;
    }

    /// <summary>The id that was requested and not found.</summary>
    public int TicketId { get; }
}
