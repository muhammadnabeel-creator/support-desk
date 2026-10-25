using SupportDesk.Api.Features.Tickets.Models;

namespace SupportDesk.Api.Features.Tickets.Dtos;

/// <summary>Body of <c>GET /api/tickets</c> and of the <c>list_tickets</c> MCP tool: one page of tickets.</summary>
/// <typeparam name="T">The item type, for example <see cref="TicketResponse"/>.</typeparam>
public sealed record PagedResponse<T>
{
    /// <summary>The items on this page, newest first; empty when the page is past the end.</summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>The 1-based page number returned.</summary>
    /// <example>1</example>
    public required int Page { get; init; }

    /// <summary>Items per page.</summary>
    /// <example>50</example>
    public required int PageSize { get; init; }

    /// <summary>Items matching the filters across all pages.</summary>
    /// <example>137</example>
    public required int TotalCount { get; init; }

    /// <summary>Number of pages at this page size; 0 when nothing matches.</summary>
    /// <example>3</example>
    public required int TotalPages { get; init; }

    /// <summary>Maps a page of results, converting each item with <paramref name="map"/>.</summary>
    /// <typeparam name="TSource">The service's item type.</typeparam>
    /// <param name="result">The page from the service.</param>
    /// <param name="map">Converts one item to its API representation.</param>
    /// <returns>The response body.</returns>
    public static PagedResponse<T> From<TSource>(PagedResult<TSource> result, Func<TSource, T> map)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(map);
        return new PagedResponse<T>
        {
            Items = result.Items.Select(map).ToList(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages,
        };
    }
}
