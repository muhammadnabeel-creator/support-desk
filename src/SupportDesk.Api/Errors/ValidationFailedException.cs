namespace SupportDesk.Api.Errors;

/// <summary>
/// Raised when request input breaks a business rule (BR1.1, BR1.2, BR4.1, BR5.2, ...).
/// Carries every failing field, keyed by its camelCase JSON name (for example <c>title</c>),
/// so the global exception handler can return one 400 ProblemDetails with an <c>errors</c> map.
/// </summary>
public class ValidationFailedException : Exception
{
    /// <summary>Creates the failure from a map of field name to error messages.</summary>
    /// <param name="errors">Field name (camelCase) to its error messages; must not be empty.</param>
    public ValidationFailedException(IReadOnlyDictionary<string, string[]> errors)
        : base(BuildMessage(errors))
    {
        Errors = new Dictionary<string, string[]>(errors, StringComparer.Ordinal);
    }

    /// <summary>Creates the failure for a single field.</summary>
    /// <param name="field">The camelCase field name, for example <c>status</c>.</param>
    /// <param name="message">The error message for that field.</param>
    public ValidationFailedException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = [message] })
    {
    }

    /// <summary>Field name (camelCase) to its error messages.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    private static string BuildMessage(IReadOnlyDictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
        {
            throw new ArgumentException("At least one field error is required.", nameof(errors));
        }

        return $"Validation failed for: {string.Join(", ", errors.Keys)}.";
    }
}
