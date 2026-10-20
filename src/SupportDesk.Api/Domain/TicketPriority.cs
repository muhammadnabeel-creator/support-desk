namespace SupportDesk.Api.Domain;

/// <summary>
/// Priority of a ticket (contract C3). Stored and transmitted by name, never by number.
/// </summary>
public enum TicketPriority
{
    /// <summary>Can wait; little or no impact on the customer.</summary>
    Low,

    /// <summary>Normal priority. Every new ticket starts with this priority.</summary>
    Medium,

    /// <summary>Important; the customer is noticeably affected.</summary>
    High,

    /// <summary>Urgent; the customer cannot work or a core service is down.</summary>
    Critical,
}
