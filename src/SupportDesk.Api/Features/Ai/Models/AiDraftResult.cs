namespace SupportDesk.Api.Features.Ai.Models;

/// <summary>
/// A validated reply draft (contract C2). Only <see cref="Validation.AiOutputValidator"/> creates it from provider output,
/// so the text is trimmed and 1-4000 characters long (BR9.1). It is a draft for human review and is never sent.
/// </summary>
/// <param name="Draft">The draft reply text.</param>
public sealed record AiDraftResult(string Draft);
