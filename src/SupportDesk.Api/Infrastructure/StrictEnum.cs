namespace SupportDesk.Api.Infrastructure;

/// <summary>
/// Parses enum values strictly by name, case-insensitively (contract C1 shared conventions).
/// Unlike <see cref="Enum.TryParse{TEnum}(string?, bool, out TEnum)"/>, numeric text such as <c>"1"</c>,
/// comma-separated combinations such as <c>"Open,Closed"</c> and undefined names are all rejected,
/// so a value can only ever become a defined member (BR2.3, BR5.2).
/// Used by the JSON enum converter and by the query-string filters.
/// </summary>
public static class StrictEnum
{
    /// <summary>Tries to match <paramref name="text"/> to one of the enum's names, ignoring case.</summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="text">The text to parse; it must equal a member name apart from letter case.</param>
    /// <param name="value">The matching member, or the default value when there is no match.</param>
    /// <returns><c>true</c> when the text names a defined member.</returns>
    public static bool TryParse<TEnum>(string? text, out TEnum value)
        where TEnum : struct, Enum
    {
        if (!string.IsNullOrEmpty(text))
        {
            foreach (var name in Enum.GetNames<TEnum>())
            {
                if (string.Equals(name, text, StringComparison.OrdinalIgnoreCase))
                {
                    value = Enum.Parse<TEnum>(name);
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    /// <summary>The enum's member names as a comma-separated list, for error messages.</summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <returns>For example <c>Open, InProgress, Resolved, Closed</c>.</returns>
    public static string AllowedValues<TEnum>()
        where TEnum : struct, Enum => string.Join(", ", Enum.GetNames<TEnum>());

    /// <summary>The member names of <paramref name="enumType"/> as a comma-separated list.</summary>
    /// <param name="enumType">An enum type.</param>
    /// <returns>For example <c>Low, Medium, High, Critical</c>.</returns>
    public static string AllowedValues(Type enumType)
    {
        ArgumentNullException.ThrowIfNull(enumType);
        return string.Join(", ", Enum.GetNames(enumType));
    }
}
