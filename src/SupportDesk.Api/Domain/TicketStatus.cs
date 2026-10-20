namespace SupportDesk.Api.Domain;

/// <summary>
/// Lifecycle status of a ticket (contract C3). Stored and transmitted by name, never by number.
/// </summary>
public enum TicketStatus
{
    /// <summary>New ticket that nobody has started on yet. Every ticket is created with this status.</summary>
    Open,

    /// <summary>An agent is working on the ticket.</summary>
    InProgress,

    /// <summary>The issue has been solved and is waiting for confirmation or closure.</summary>
    Resolved,

    /// <summary>The ticket is finished; no further work is expected.</summary>
    Closed,
}
