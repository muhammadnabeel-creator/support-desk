namespace SupportDesk.Api.Features.Tickets.Models;

/// <summary>
/// Which page of the ticket list to return (FR3.3, page-based paging). A page is not stored anywhere: it is the
/// slice of the filtered, sorted list that starts after <c>(Page - 1) * PageSize</c> tickets.
/// </summary>
/// <param name="Page">The 1-based page number; <c>null</c> means <see cref="DefaultPage"/>.</param>
/// <param name="PageSize">Tickets per page; <c>null</c> means <see cref="DefaultPageSize"/>.</param>
public sealed record PageRequest(int? Page = null, int? PageSize = null)
{
    /// <summary>The page returned when none is given.</summary>
    public const int DefaultPage = 1;

    /// <summary>Tickets per page when no size is given.</summary>
    public const int DefaultPageSize = 50;

    /// <summary>The largest page size allowed, so no request can return an unbounded list.</summary>
    public const int MaxPageSize = 200;

    /// <summary>The first page with the default size.</summary>
    public static PageRequest First { get; } = new();
}
