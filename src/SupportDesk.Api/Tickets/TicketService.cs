using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Tickets;

/// <summary>
/// Implements the ticket rules (BR1.1-BR5.2) with EF Core directly (ADR-004). Depends only on the
/// database and the clock, never on the AI service (BR6.5). Validation collects every failing field
/// before raising one <see cref="ValidationFailedException"/>, whose keys are camelCase JSON names.
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

    /// <summary>Creates the service.</summary>
    /// <param name="db">The database context (scoped per request).</param>
    /// <param name="clock">Supplies UTC timestamps.</param>
    public TicketService(SupportDeskDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
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
