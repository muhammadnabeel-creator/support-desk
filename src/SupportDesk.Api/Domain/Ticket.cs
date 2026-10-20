namespace SupportDesk.Api.Domain;

/// <summary>
/// A customer support request. Owned by the Tickets component; the only persisted entity.
/// Length limits and storage rules are configured in <c>SupportDeskDbContext</c>.
/// </summary>
public class Ticket
{
    /// <summary>Maximum length of <see cref="Title"/>.</summary>
    public const int TitleMaxLength = 200;

    /// <summary>Maximum length of <see cref="Description"/>.</summary>
    public const int DescriptionMaxLength = 4000;

    /// <summary>Maximum length of <see cref="AiSummary"/>.</summary>
    public const int AiSummaryMaxLength = 500;

    /// <summary>Maximum length of <see cref="SuggestedAction"/>.</summary>
    public const int SuggestedActionMaxLength = 500;

    /// <summary>Maximum length of <see cref="DraftReply"/>.</summary>
    public const int DraftReplyMaxLength = 4000;

    /// <summary>System-generated, positive identifier; never reused.</summary>
    public int Id { get; set; }

    /// <summary>Short summary of the request (3–200 characters, stored trimmed).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Full description of the request (10–4000 characters, stored trimmed).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Lifecycle status; new tickets start as <see cref="TicketStatus.Open"/>.</summary>
    public TicketStatus Status { get; set; } = TicketStatus.Open;

    /// <summary>Priority; defaults to <see cref="TicketPriority.Medium"/>.</summary>
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    /// <summary>Category set by AI analysis (U2); null until analyzed.</summary>
    public TicketCategory? Category { get; set; }

    /// <summary>Sentiment set by AI analysis (U2); null until analyzed.</summary>
    public TicketSentiment? Sentiment { get; set; }

    /// <summary>Short AI summary (max 500 characters); null until analyzed.</summary>
    public string? AiSummary { get; set; }

    /// <summary>AI-suggested next action (max 500 characters); null until analyzed.</summary>
    public string? SuggestedAction { get; set; }

    /// <summary>Latest AI draft reply (max 4000 characters). A draft only; never sent.</summary>
    public string? DraftReply { get; set; }

    /// <summary>UTC time the draft reply was generated; set together with <see cref="DraftReply"/>.</summary>
    public DateTime? DraftReplyGeneratedAt { get; set; }

    /// <summary>UTC creation time; set once and never changed.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC time of the last change; never earlier than <see cref="CreatedAt"/>.</summary>
    public DateTime UpdatedAt { get; set; }
}
