using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Features.Ai;
using SupportDesk.Api.Features.Ai.Models;
using SupportDesk.Api.Features.Tickets.Models;
using SupportDesk.Api.Features.Tickets.Validation;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Features.Tickets;

/// <summary>
/// Implements the ticket rules (BR1.1-BR5.2, BR7.1-BR9.4) with EF Core directly (ADR-004). The AI service is
/// used only by <see cref="AnalyzeAsync"/> and <see cref="DraftReplyAsync"/>; the CRUD operations depend on the
/// database and the clock alone (BR6.5, NFR1.2). Input is checked with FluentValidation validators (in
/// <c>Validation/</c>) that report every failing field in one <see cref="ValidationFailedException"/>, whose keys are
/// camelCase JSON names. Because the checks run here, REST and MCP are validated identically (FR15.1). AI failures are not caught here:
/// they propagate unchanged to the global exception handler, and because they happen before any field is
/// assigned, nothing is saved (BR7.6).
/// </summary>
public sealed class TicketService : ITicketService
{
    /// <summary>Minimum title length after trimming (BR1.1).</summary>
    public const int TitleMinLength = 3;

    /// <summary>Minimum description length after trimming (BR1.2).</summary>
    public const int DescriptionMinLength = 10;

    // FluentValidation validators hold no state and are thread-safe, so one instance of each is shared.
    private static readonly CreateTicketInputValidator _createValidator = new();
    private static readonly UpdateTicketInputValidator _updateValidator = new();
    private static readonly ChangeStatusInputValidator _statusValidator = new();
    private static readonly TicketListFilterValidator _filterValidator = new();
    private static readonly PageRequestValidator _pagingValidator = new();

    private readonly SupportDeskDbContext _db;
    private readonly IClock _clock;
    private readonly IAiTicketService _ai;
    private readonly IAiCallLimiter _aiLimiter;

    /// <summary>Creates the service.</summary>
    /// <param name="db">The database context (scoped per request).</param>
    /// <param name="clock">Supplies UTC timestamps.</param>
    /// <param name="ai">The AI assistant; used only by the analyze and draft-reply operations.</param>
    /// <param name="aiLimiter">The AI request budget, checked before every AI call.</param>
    public TicketService(SupportDeskDbContext db, IClock clock, IAiTicketService ai, IAiCallLimiter aiLimiter)
    {
        _db = db;
        _clock = clock;
        _ai = ai;
        _aiLimiter = aiLimiter;
    }

    /// <inheritdoc />
    public async Task<Ticket> CreateAsync(CreateTicketInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        _createValidator.ValidateOrThrow(input);

        var now = _clock.UtcNow;
        var ticket = new Ticket
        {
            Title = input.Title!.Trim(),
            Description = input.Description!.Trim(),
            Status = TicketStatus.Open,
            Priority = TicketPriority.Medium,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    /// <inheritdoc />
    public async Task<PagedResult<Ticket>> ListAsync(
        TicketListFilter filter,
        PageRequest paging,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(paging);
        _filterValidator.ValidateOrThrow(filter);
        _pagingValidator.ValidateOrThrow(paging);
        var page = paging.Page ?? PageRequest.DefaultPage;
        var pageSize = paging.PageSize ?? PageRequest.DefaultPageSize;

        var query = _db.Tickets.AsNoTracking();
        if (filter.Status is { } status)
        {
            query = query.Where(t => t.Status == status);
        }

        if (filter.Priority is { } priority)
        {
            query = query.Where(t => t.Priority == priority);
        }

        if (filter.Category is { } category)
        {
            query = query.Where(t => t.Category == category);
        }

        // The count and the page are computed by SQLite; only the requested page is loaded into memory.
        // The sort includes the id so the order is strict and every page is repeatable.
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Ticket>(items, page, pageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<Ticket> GetAsync(int id, CancellationToken cancellationToken)
    {
        var ticket = await _db.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return ticket ?? throw new TicketNotFoundException(id);
    }

    /// <inheritdoc />
    public async Task<Ticket> UpdateAsync(int id, UpdateTicketInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Validate everything first, so an invalid request never changes the ticket (BR4.1).
        _updateValidator.ValidateOrThrow(input);

        var ticket = await LoadTrackedAsync(id, cancellationToken);

        // Only these three fields change; status and AI fields are left as they are (BR4.2).
        ticket.Title = input.Title!.Trim();
        ticket.Description = input.Description!.Trim();
        ticket.Priority = input.Priority!.Value;
        ticket.UpdatedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    /// <inheritdoc />
    public async Task<Ticket> ChangeStatusAsync(int id, TicketStatus? status, CancellationToken cancellationToken)
    {
        _statusValidator.ValidateOrThrow(new ChangeStatusInput(status));

        var ticket = await LoadTrackedAsync(id, cancellationToken);

        // Any status may follow any status, including the current one (BR5.1).
        ticket.Status = status!.Value;
        ticket.UpdatedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    /// <inheritdoc />
    public async Task<TicketAnalysisOutcome> AnalyzeAsync(int id, CancellationToken cancellationToken)
    {
        // Load first, so an unknown id is a 404 and the AI is never called (BR7.2). Status is not checked (BR7.1).
        var ticket = await LoadTrackedAsync(id, cancellationToken);

        // Within the AI request budget (429 otherwise); then only title and description go to the AI (BR7.3).
        // If either throws, nothing has been changed (BR7.6).
        _aiLimiter.Acquire();
        var analysis = await _ai.AnalyzeAsync(ticket.Title, ticket.Description, cancellationToken);

        // Replace the whole earlier analysis (BR7.5) and overwrite the priority (BR7.4). Status is never touched (BR8.5).
        var previousPriority = ticket.Priority;
        ticket.Category = analysis.Category;
        ticket.Sentiment = analysis.Sentiment;
        ticket.AiSummary = analysis.Summary;
        ticket.SuggestedAction = analysis.SuggestedAction;
        ticket.Priority = analysis.Priority;
        ticket.UpdatedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return new TicketAnalysisOutcome(ticket, previousPriority);
    }

    /// <inheritdoc />
    public async Task<TicketDraft> DraftReplyAsync(int id, CancellationToken cancellationToken)
    {
        // Read-only: load first, so an unknown id is a 404 and the AI is never called (BR7.2). Status is not checked.
        var ticket = await GetAsync(id, cancellationToken);

        // Within the AI request budget (429 otherwise). The AI result is already validated (BR9.1).
        _aiLimiter.Acquire();
        var draft = await _ai.DraftReplyAsync(
            ticket.Title,
            ticket.Description,
            CreateDraftContext(ticket),
            cancellationToken);

        // Returned for human review only (FR-07): nothing is saved and nothing is sent (BR9.4).
        return new TicketDraft(ticket.Id, draft.Draft, _clock.UtcNow);
    }

    /// <summary>
    /// Builds the saved-analysis context for a draft, or null when the ticket has not been analyzed
    /// (<c>Category</c> is not set). The current priority is not part of it (reliability design, Draft Input).
    /// </summary>
    private static AiDraftContext? CreateDraftContext(Ticket ticket) =>
        ticket.Category is { } category
            ? new AiDraftContext(category, ticket.Sentiment, ticket.AiSummary, ticket.SuggestedAction)
            : null;

    private async Task<Ticket> LoadTrackedAsync(int id, CancellationToken cancellationToken)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        return ticket ?? throw new TicketNotFoundException(id);
    }
}
