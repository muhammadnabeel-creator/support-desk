namespace SupportDesk.Api.Features.Tickets.Models;

/// <summary>One page of results plus what a client needs to ask for the others.</summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The items on this page; empty when the page is past the end.</param>
/// <param name="Page">The 1-based page number that was returned.</param>
/// <param name="PageSize">The page size that was used.</param>
/// <param name="TotalCount">How many items match the filters in total, across all pages.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>How many pages there are at this page size; 0 when nothing matches.</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
