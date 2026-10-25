using Microsoft.AspNetCore.Mvc;
using SupportDesk.Api.Features.Dashboard.Dtos;

namespace SupportDesk.Api.Features.Dashboard;

/// <summary>
/// Dashboard endpoint (contract C1, US3.1). Thin: it calls <see cref="IDashboardService"/> and maps the result.
/// Never calls the AI, so it works when the AI provider is down or not configured.
/// </summary>
[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    /// <summary>Creates the controller.</summary>
    /// <param name="dashboard">The dashboard service.</param>
    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    /// <summary>Returns ticket counts for the support lead (US3.1).</summary>
    /// <remarks>
    /// <c>open</c> counts only status Open (InProgress has its own count); <c>highPriority</c> counts High and
    /// Critical tickets in any status; <c>unanalyzed</c> counts tickets with no category yet. All counts are 0
    /// when there are no tickets.
    /// </remarks>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <returns>The counts.</returns>
    /// <response code="200">The counts.</response>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var summary = await _dashboard.GetSummaryAsync(cancellationToken);
        return Ok(DashboardSummaryResponse.FromSummary(summary));
    }
}
