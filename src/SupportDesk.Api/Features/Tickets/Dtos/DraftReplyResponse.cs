using SupportDesk.Api.Features.Tickets.Models;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Features.Tickets.Dtos;

/// <summary>
/// Body of a successful <c>POST /api/tickets/{id}/draft-reply</c> (contract C1 <c>DraftReplyResponse</c>, BR9.3).
/// The draft is returned for a human to review and edit; it is not stored and never sent (FR-07, BR9.4). The field is called
/// <c>notice</c>, not <c>status</c>, so it cannot be confused with the ticket's status.
/// </summary>
public sealed record DraftReplyResponse
{
    /// <summary>The fixed notice returned with every draft (BR9.3).</summary>
    public const string DraftNotice = "Draft — requires human review; not sent";

    /// <summary>The id of the ticket the draft belongs to.</summary>
    /// <example>1</example>
    public required int TicketId { get; init; }

    /// <summary>The draft reply text: 1-4000 characters. Not sent to anyone.</summary>
    public required string Draft { get; init; }

    /// <summary>UTC time the draft was generated. The draft is not stored on the ticket.</summary>
    public required DateTime GeneratedAt { get; init; }

    /// <summary>Always <c>Draft — requires human review; not sent</c>.</summary>
    /// <example>Draft — requires human review; not sent</example>
    public string Notice => DraftNotice;

    /// <summary>Maps a generated draft to its API representation.</summary>
    /// <param name="draft">The draft from the ticket service.</param>
    /// <returns>The response with the draft, its UTC timestamp and the fixed notice.</returns>
    public static DraftReplyResponse FromDraft(TicketDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        return new DraftReplyResponse
        {
            TicketId = draft.TicketId,
            Draft = draft.Draft,
            GeneratedAt = UtcDateTimeConverter.ToUtc(draft.GeneratedAt),
        };
    }
}
