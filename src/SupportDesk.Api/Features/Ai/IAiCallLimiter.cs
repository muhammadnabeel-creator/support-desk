using SupportDesk.Api.Errors;

namespace SupportDesk.Api.Features.Ai;

/// <summary>
/// Limits how many AI requests (analyze and draft-reply) can start per minute, so a script, a mistake or a
/// looping MCP client cannot use up the Groq quota. Checked by the ticket service, so REST and MCP share one budget.
/// </summary>
public interface IAiCallLimiter
{
    /// <summary>Takes one AI request from the budget.</summary>
    /// <exception cref="AiRateLimitedException">The budget for the current minute is used up.</exception>
    void Acquire();
}
