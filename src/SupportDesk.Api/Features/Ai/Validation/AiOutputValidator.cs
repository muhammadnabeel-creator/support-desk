using System.Text;
using System.Text.Json;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Features.Ai.Models;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Features.Ai.Validation;

/// <summary>
/// Turns the provider's message content into a validated <see cref="AiAnalysisResult"/> or
/// <see cref="AiDraftResult"/> (BR8.1-BR8.4, BR9.1). AI output is untrusted data: anything that does not match
/// the expected shape and allowed values is rejected with <see cref="AiInvalidOutputException"/>, so it is never
/// saved or returned. The exception message is fixed and generic; it never contains AI text (NFR2.15).
/// </summary>
public static class AiOutputValidator
{
    /// <summary>Maximum length of a reply draft (BR9.1).</summary>
    public const int DraftMaxLength = 4000;

    /// <summary>The fixed message of every <see cref="AiInvalidOutputException"/> raised here.</summary>
    public const string InvalidOutputMessage = "The AI response was not valid.";

    /// <summary>Validates analysis content: one JSON object with the five known fields (BR8.1-BR8.4).</summary>
    /// <param name="content">The provider's message content.</param>
    /// <returns>The validated analysis, with trimmed texts.</returns>
    /// <exception cref="AiInvalidOutputException">
    /// The content is not a single JSON object, an enum value is not an allowed name, or a text field is missing,
    /// not a string, empty after trimming or longer than 500 characters.
    /// </exception>
    public static AiAnalysisResult ValidateAnalysis(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw Invalid();
        }

        using var document = ParseJson(content);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw Invalid();
        }

        // Only these five fields are read; any other field in the object is ignored (BR8.4).
        return new AiAnalysisResult(
            ReadEnum<TicketCategory>(root, "category"),
            ReadEnum<TicketPriority>(root, "priority"),
            ReadEnum<TicketSentiment>(root, "sentiment"),
            ReadText(root, "summary", Ticket.AiSummaryMaxLength),
            ReadText(root, "suggestedAction", Ticket.SuggestedActionMaxLength));
    }

    /// <summary>Validates draft content: trimmed text of 1-4000 characters (BR9.1).</summary>
    /// <param name="content">The provider's message content.</param>
    /// <returns>The validated draft, trimmed.</returns>
    /// <exception cref="AiInvalidOutputException">The content is missing, whitespace-only or too long.</exception>
    public static AiDraftResult ValidateDraft(string? content)
    {
        var draft = content?.Trim();
        if (string.IsNullOrEmpty(draft) || draft.Length > DraftMaxLength)
        {
            throw Invalid();
        }

        return new AiDraftResult(draft);
    }

    private static JsonDocument ParseJson(string content)
    {
        try
        {
            return JsonDocument.Parse(content);
        }
        catch (JsonException)
        {
            // The parser's message can quote part of the AI text, so it is not attached (NFR2.15).
            throw Invalid();
        }
    }

    private static TEnum ReadEnum<TEnum>(JsonElement root, string propertyName)
        where TEnum : struct, Enum
    {
        // Only JSON strings are accepted; numbers such as 1 are rejected (BR8.2).
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
        {
            throw Invalid();
        }

        // StrictEnum matches member names only (via Enum.GetNames), so numeric text such as "1" never matches.
        var normalized = NormalizeEnumText(element.GetString());
        if (!StrictEnum.TryParse<TEnum>(normalized, out var value))
        {
            throw Invalid();
        }

        return value;
    }

    private static string ReadText(JsonElement root, string propertyName, int maxLength)
    {
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
        {
            throw Invalid();
        }

        var text = element.GetString()?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > maxLength)
        {
            throw Invalid();
        }

        return text;
    }

    /// <summary>Removes whitespace, hyphens and underscores, so "account access" or "Technical-Issue" match.</summary>
    private static string NormalizeEnumText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            if (!char.IsWhiteSpace(character) && character != '-' && character != '_')
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static AiInvalidOutputException Invalid() => new(InvalidOutputMessage);
}
