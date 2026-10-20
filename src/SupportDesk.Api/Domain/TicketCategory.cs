namespace SupportDesk.Api.Domain;

/// <summary>
/// Category of a ticket, set only by AI analysis (contract C3). Stored and transmitted by name.
/// </summary>
public enum TicketCategory
{
    /// <summary>Login, password, or account access problems.</summary>
    AccountAccess,

    /// <summary>Invoices, payments, charges, or refunds.</summary>
    Billing,

    /// <summary>A bug, error, or something in the product that does not work.</summary>
    TechnicalIssue,

    /// <summary>A request for a new feature or a change to existing behaviour.</summary>
    FeatureRequest,

    /// <summary>A general question that is not a problem report.</summary>
    GeneralInquiry,

    /// <summary>Anything that fits none of the other categories.</summary>
    Other,
}
