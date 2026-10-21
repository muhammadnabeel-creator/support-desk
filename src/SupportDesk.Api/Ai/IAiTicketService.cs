using SupportDesk.Api.Errors;

namespace SupportDesk.Api.Ai;

/// <summary>
/// The AI assistant (contract C2). Callers receive only validated results or one of four typed failures,
/// never raw provider text. Only the ticket's title and description (and, for drafts, its saved analysis)
/// are sent to the provider; the ticket id and timestamps never are (BR7.3).
/// </summary>
public interface IAiTicketService
{
    /// <summary>Classifies a ticket and suggests a next action (WF7).</summary>
    /// <param name="title">The ticket title.</param>
    /// <param name="description">The ticket description.</param>
    /// <param name="cancellationToken">Cancels the AI call when the client disconnects.</param>
    /// <returns>The validated analysis (BR8.1-BR8.4).</returns>
    /// <exception cref="AiNotConfiguredException">No API key is configured (BR10.1).</exception>
    /// <exception cref="AiTimeoutException">The provider did not answer within the timeout (BR10.2).</exception>
    /// <exception cref="AiProviderException">The provider returned an error or could not be reached (BR10.3).</exception>
    /// <exception cref="AiInvalidOutputException">The provider's answer failed validation.</exception>
    Task<AiAnalysisResult> AnalyzeAsync(string title, string description, CancellationToken cancellationToken);

    /// <summary>Writes a reply draft for human review; nothing is ever sent (WF8, BR9.4).</summary>
    /// <param name="title">The ticket title.</param>
    /// <param name="description">The ticket description.</param>
    /// <param name="analysis">The ticket's saved analysis, or null when it has not been analyzed.</param>
    /// <param name="cancellationToken">Cancels the AI call when the client disconnects.</param>
    /// <returns>The validated draft (BR9.1).</returns>
    /// <exception cref="AiNotConfiguredException">No API key is configured (BR10.1).</exception>
    /// <exception cref="AiTimeoutException">The provider did not answer within the timeout (BR10.2).</exception>
    /// <exception cref="AiProviderException">The provider returned an error or could not be reached (BR10.3).</exception>
    /// <exception cref="AiInvalidOutputException">The provider's answer failed validation.</exception>
    Task<AiDraftResult> DraftReplyAsync(
        string title,
        string description,
        AiDraftContext? analysis,
        CancellationToken cancellationToken);
}
