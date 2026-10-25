using SupportDesk.Api.Domain;
using SupportDesk.Api.Features.Ai;
using SupportDesk.Api.Features.Ai.Models;

namespace SupportDesk.Api.Tests.TestSupport;

/// <summary>
/// A hand-written AI fake (team practice: tests never call a real AI provider). By default it returns a valid
/// analysis and draft; a test can make it throw any failure instead. It records how it was called.
/// </summary>
public sealed class FakeAiTicketService : IAiTicketService
{
    public static readonly AiAnalysisResult DefaultAnalysis = new(
        TicketCategory.AccountAccess,
        TicketPriority.High,
        TicketSentiment.Frustrated,
        "The customer cannot sign in after a password reset.",
        "Check the account status and the reset flow.");

    public const string DefaultDraft = "Hello, thank you for contacting us. We are looking into your sign-in problem.";

    public AiAnalysisResult Analysis { get; set; } = DefaultAnalysis;

    public string Draft { get; set; } = DefaultDraft;

    /// <summary>When set, every call throws this instead of returning a result.</summary>
    public Exception? Failure { get; set; }

    public int AnalyzeCalls { get; private set; }

    public int DraftCalls { get; private set; }

    public string? LastTitle { get; private set; }

    public AiDraftContext? LastDraftContext { get; private set; }

    public Task<AiAnalysisResult> AnalyzeAsync(string title, string description, CancellationToken cancellationToken)
    {
        AnalyzeCalls++;
        LastTitle = title;
        return Failure is null ? Task.FromResult(Analysis) : Task.FromException<AiAnalysisResult>(Failure);
    }

    public Task<AiDraftResult> DraftReplyAsync(
        string title,
        string description,
        AiDraftContext? analysis,
        CancellationToken cancellationToken)
    {
        DraftCalls++;
        LastTitle = title;
        LastDraftContext = analysis;
        return Failure is null ? Task.FromResult(new AiDraftResult(Draft)) : Task.FromException<AiDraftResult>(Failure);
    }
}
