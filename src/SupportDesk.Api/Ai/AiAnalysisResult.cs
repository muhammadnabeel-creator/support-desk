using SupportDesk.Api.Domain;

namespace SupportDesk.Api.Ai;

/// <summary>
/// A validated AI analysis (contract C2). Only <see cref="AiOutputValidator"/> creates it from provider output,
/// so every value is an allowed enum member and both texts are trimmed and 1-500 characters long (BR8.2, BR8.3).
/// </summary>
/// <param name="Category">The ticket category.</param>
/// <param name="Priority">The suggested priority; it overwrites the ticket's priority (BR7.4).</param>
/// <param name="Sentiment">The customer's sentiment.</param>
/// <param name="Summary">A short summary of the ticket.</param>
/// <param name="SuggestedAction">The suggested next action for the support agent.</param>
public sealed record AiAnalysisResult(
    TicketCategory Category,
    TicketPriority Priority,
    TicketSentiment Sentiment,
    string Summary,
    string SuggestedAction);
