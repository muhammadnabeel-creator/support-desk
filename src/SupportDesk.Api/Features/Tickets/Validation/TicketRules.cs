using FluentValidation;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Features.Tickets.Validation;

/// <summary>
/// Reusable FluentValidation rules for ticket input (BR1.1, BR1.2, BR4.1, BR5.2) and the bridge from a
/// FluentValidation result to <see cref="ValidationFailedException"/>, so failures keep the shared 400
/// ProblemDetails shape for REST and MCP alike.
/// </summary>
public static class TicketRules
{
    /// <summary>
    /// Text that is required and, after trimming, between <paramref name="minLength"/> and
    /// <paramref name="maxLength"/> characters. Whitespace-only counts as missing.
    /// </summary>
    /// <typeparam name="T">The validated type.</typeparam>
    /// <param name="rule">The rule for the text property.</param>
    /// <param name="label">The field's display name in messages, for example <c>Title</c>.</param>
    /// <param name="minLength">The smallest allowed trimmed length.</param>
    /// <param name="maxLength">The largest allowed trimmed length.</param>
    /// <returns>The rule, for chaining.</returns>
    public static IRuleBuilderOptions<T, string?> TrimmedText<T>(
        this IRuleBuilder<T, string?> rule,
        string label,
        int minLength,
        int maxLength) =>
        rule
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage($"{label} is required.")
            .Must(value => value!.Trim().Length >= minLength && value.Trim().Length <= maxLength)
            .WithMessage($"{label} must be between {minLength} and {maxLength} characters after trimming.");

    /// <summary>A required enum value that must be a defined member.</summary>
    /// <typeparam name="T">The validated type.</typeparam>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="rule">The rule for the enum property.</param>
    /// <param name="label">The field's display name in messages, for example <c>Status</c>.</param>
    /// <returns>The rule, for chaining.</returns>
    public static IRuleBuilderOptions<T, TEnum?> RequiredEnum<T, TEnum>(
        this IRuleBuilder<T, TEnum?> rule,
        string label)
        where TEnum : struct, Enum =>
        rule
            .NotNull()
            .WithMessage($"{label} is required. Allowed values: {StrictEnum.AllowedValues<TEnum>()}.")
            .Must(value => Enum.IsDefined(value!.Value))
            .WithMessage($"{label} must be one of: {StrictEnum.AllowedValues<TEnum>()}.");

    /// <summary>An optional enum value that, when present, must be a defined member.</summary>
    /// <typeparam name="T">The validated type.</typeparam>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="rule">The rule for the enum property.</param>
    /// <param name="label">The field's display name in messages, for example <c>Category</c>.</param>
    /// <returns>The rule, for chaining.</returns>
    public static IRuleBuilderOptions<T, TEnum?> OptionalEnum<T, TEnum>(
        this IRuleBuilder<T, TEnum?> rule,
        string label)
        where TEnum : struct, Enum =>
        rule
            .Must(value => value is null || Enum.IsDefined(value.Value))
            .WithMessage($"{label} must be one of: {StrictEnum.AllowedValues<TEnum>()}.");

    /// <summary>
    /// Validates <paramref name="instance"/> and throws <see cref="ValidationFailedException"/> with every failing
    /// field (camelCase name to messages) when it is invalid.
    /// </summary>
    /// <typeparam name="T">The validated type.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="instance">The input to check.</param>
    /// <exception cref="ValidationFailedException">One or more fields are invalid; nothing has been changed.</exception>
    public static void ValidateOrThrow<T>(this IValidator<T> validator, T instance)
    {
        ArgumentNullException.ThrowIfNull(validator);

        var result = validator.Validate(instance);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray(),
                StringComparer.Ordinal);
        throw new ValidationFailedException(errors);
    }
}
