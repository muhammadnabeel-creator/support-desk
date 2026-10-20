namespace SupportDesk.Api.Domain;

/// <summary>
/// Customer sentiment of a ticket, set only by AI analysis (contract C3). Stored and transmitted by name.
/// </summary>
public enum TicketSentiment
{
    /// <summary>The customer sounds satisfied or friendly.</summary>
    Positive,

    /// <summary>The customer sounds matter-of-fact, neither pleased nor upset.</summary>
    Neutral,

    /// <summary>The customer sounds unhappy or dissatisfied.</summary>
    Negative,

    /// <summary>The customer sounds clearly annoyed or exasperated, often after repeated problems.</summary>
    Frustrated,
}
