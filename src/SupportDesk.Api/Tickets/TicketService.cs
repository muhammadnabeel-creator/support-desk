using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Ai;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Tickets;

/// <summary>
/// Implements the ticket rules (BR1.1-BR5.2, BR7.1-BR9.4) with EF Core directly (ADR-004). The AI service is
/// used only by <see cref="AnalyzeAsync"/> and <see cref="DraftReplyAsync"/>; the CRUD operations depend on the
/// database and the clock alone (BR6.5, NFR1.2). Validation collects every failing field before raising one
/// <see cref="ValidationFailedException"/>, whose keys are camelCase JSON names. AI failures are not caught here:
/// they propagate unchanged to the global exception handler, and because they happen before any field is
/// assigned, nothing is saved (BR7.6).
/// </summary>
public sealed class TicketService : ITicketService
{
    /// <summary>Minimum title length after trimming (BR1.1).</summary>
    public const int TitleMinLength = 3;

    /// <summary>Minimum description length after trimming (BR1.2).</summary>
    public const int DescriptionMinLength = 10;

    private const string TitleField = "title";
    private const string DescriptionField = "description";
    private const string PriorityField = "priority";
    private const string StatusField = "status";
    private const string CategoryField = "category";

    private readonly SupportDeskDbContext _db;
    private readonly IClock _clock;
    private readonly IAiTicketService _ai;

    /// <summary>Creates the service.</summary>
    /// <param name="db">The database context (scoped per request).</param>
    /// <param name="clock">Supplies UTC timestamps.</param>
    /// <param name="ai">The AI assistant; used only by the analyze and draft-reply operations.</param>
    public TicketService(SupportDeskDbContext db, IClock clock, IAiTicketService ai)
    {
        _db = db;
        _clock = clock;
        _ai = ai;
    }

    /// <inheritdoc />
    public async Task<Ticket> CreateAsync(CreateTicketInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var title = ValidateTitle(input.Title, errors);
        var description = ValidateDescription(input.Description, errors);
        ThrowIfAny(errors);

        var now = _clock.UtcNow;
        var ticket = new Ticket
        {
            Title = title!,
            Description = description!,
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
    public async Task<IReadOnlyList<Ticket>> ListAsync(TicketListFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ValidateFilter(filter);

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

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .ToListAsync(cancellationToken);
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
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var title = ValidateTitle(input.Title, errors);
        var description = ValidateDescription(input.Description, errors);
        var priority = ValidateRequiredEnum(input.Priority, PriorityField, "Priority", errors);
        ThrowIfAny(errors);

        var ticket = await LoadTrackedAsync(id, cancellationToken);

        // Only these three fields change; status and AI fields are left as they are (BR4.2).
        ticket.Title = title!;
        ticket.Description = description!;
        ticket.Priority = priority!.Value;
        ticket.UpdatedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    /// <inheritdoc />
    public async Task<Ticket> ChangeStatusAsync(int id, TicketStatus? status, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var newStatus = ValidateRequiredEnum(status, StatusField, "Status", errors);
        ThrowIfAny(errors);

        var ticket = await LoadTrackedAsync(id, cancellationToken);

        // Any status may follow any status, including the current one (BR5.1).
        ticket.Status = newStatus!.Value;
        ticket.UpdatedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    /// <inheritdoc />
    public async Task<TicketAnalysisOutcome> AnalyzeAsync(int id, CancellationToken cancellationToken)
    {
        // Load first, so an unknown id is a 404 and the AI is never called (BR7.2). Status is not checked (BR7.1).
        var ticket = await LoadTrackedAsync(id, cancellationToken);

        // Only title and description go to the AI (BR7.3). If this throws, nothing has been changed (BR7.6).
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
    public async Task<Ticket> DraftReplyAsync(int id, CancellationToken cancellationToken)
    {
        // Load first, so an unknown id is a 404 and the AI is never called (BR7.2). Status is not checked (BR7.1).
        var ticket = await LoadTrackedAsync(id, cancellationToken);

        // If this throws, nothing has been changed (BR7.6).
        var draft = await _ai.DraftReplyAsync(
            ticket.Title,
            ticket.Description,
            CreateDraftContext(ticket),
            cancellationToken);

        // Replace any earlier draft; both timestamps share one instant (BR9.2). The draft is stored, never sent (BR9.4).
        var now = _clock.UtcNow;
        ticket.DraftReply = draft.Draft;
        ticket.DraftReplyGeneratedAt = now;
        ticket.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);
        return ticket;
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

    private static string? ValidateTitle(string? value, IDictionary<string, string[]> errors) =>
        ValidateText(value, TitleField, "Title", TitleMinLength, Ticket.TitleMaxLength, errors);

    private static string? ValidateDescription(string? value, IDictionary<string, string[]> errors) =>
        ValidateText(value, DescriptionField, "Description", DescriptionMinLength, Ticket.DescriptionMaxLength, errors);

    /// <summary>
    /// Trims the value and checks it is present and within the length range (BR1.1, BR1.2).
    /// Whitespace-only counts as missing. Returns the trimmed text, or null after recording an error.
    /// </summary>
    private static string? ValidateText(
        string? value,
        string field,
        string label,
        int minLength,
        int maxLength,
        IDictionary<string, string[]> errors)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            errors[field] = [$"{label} is required."];
            return null;
        }

        if (trimmed.Length < minLength || trimmed.Length > maxLength)
        {
            errors[field] = [$"{label} must be between {minLength} and {maxLength} characters after trimming."];
            return null;
        }

        return trimmed;
    }

    /// <summary>
    /// Checks a required enum value is present and a defined member; records an error that lists the
    /// allowed values otherwise (BR4.1, BR5.2).
    /// </summary>
    private static TEnum? ValidateRequiredEnum<TEnum>(
        TEnum? value,
        string field,
        string label,
        IDictionary<string, string[]> errors)
        where TEnum : struct, Enum
    {
        if (value is null)
        {
            errors[field] = [$"{label} is required. Allowed values: {AllowedValues<TEnum>()}."];
            return null;
        }

        if (!Enum.IsDefined(value.Value))
        {
            errors[field] = [$"{label} must be one of: {AllowedValues<TEnum>()}."];
            return null;
        }

        return value;
    }

    private static void ValidateFilter(TicketListFilter filter)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        AddIfUndefined(filter.Status, StatusField, "Status", errors);
        AddIfUndefined(filter.Priority, PriorityField, "Priority", errors);
        AddIfUndefined(filter.Category, CategoryField, "Category", errors);
        ThrowIfAny(errors);
    }

    private static void AddIfUndefined<TEnum>(
        TEnum? value,
        string field,
        string label,
        IDictionary<string, string[]> errors)
        where TEnum : struct, Enum
    {
        if (value is { } defined && !Enum.IsDefined(defined))
        {
            errors[field] = [$"{label} must be one of: {AllowedValues<TEnum>()}."];
        }
    }

    private static string AllowedValues<TEnum>()
        where TEnum : struct, Enum => string.Join(", ", Enum.GetNames<TEnum>());

    private static void ThrowIfAny(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }
    }
}
