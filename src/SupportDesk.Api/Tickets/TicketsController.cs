using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Errors;
using SupportDesk.Api.Infrastructure;
using SupportDesk.Api.Tickets.Dtos;

namespace SupportDesk.Api.Tickets;

/// <summary>
/// Ticket endpoints (contract C1, workflows WF1-WF5, WF7, WF8). Thin by design: it turns HTTP input into service
/// calls and service results into responses. Business rules live in <see cref="ITicketService"/>; every
/// failure is raised as a typed exception and written as ProblemDetails by the global exception handler.
/// </summary>
[ApiController]
[Route("api/tickets")]
public sealed class TicketsController : ControllerBase
{
    private const string GetTicketRouteName = "GetTicket";
    private const string ProblemJson = "application/problem+json";

    private readonly ITicketService _tickets;

    /// <summary>Creates the controller.</summary>
    /// <param name="tickets">The ticket service.</param>
    public TicketsController(ITicketService tickets)
    {
        _tickets = tickets;
    }

    /// <summary>Creates a ticket (US1.1).</summary>
    /// <remarks>
    /// Title and description are trimmed and then checked: title 3-200 characters, description 10-4000.
    /// A new ticket is Open with Medium priority and no AI fields; other fields in the body are ignored.
    /// </remarks>
    /// <param name="request">The title and description.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <returns>The created ticket.</returns>
    /// <response code="201">Created. The Location header points to the new ticket.</response>
    /// <response code="400">A field is missing or invalid, or the body is not valid JSON.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, ProblemJson)]
    public async Task<ActionResult<TicketResponse>> CreateAsync(
        [FromBody] CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var ticket = await _tickets.CreateAsync(
            new CreateTicketInput(request.Title, request.Description),
            cancellationToken);

        return CreatedAtRoute(GetTicketRouteName, new { id = ticket.Id }, TicketResponse.FromTicket(ticket));
    }

    /// <summary>Lists tickets, newest first (US1.2).</summary>
    /// <remarks>
    /// Tickets are ordered by creation time, newest first; tickets created at the same time are ordered by id,
    /// highest first. Filters are optional, combined with AND, and matched case-insensitively against enum
    /// names. An empty filter value (for example <c>?status=</c>) is ignored.
    /// </remarks>
    /// <param name="status">Keep only tickets with this status: Open, InProgress, Resolved or Closed.</param>
    /// <param name="priority">Keep only tickets with this priority: Low, Medium, High or Critical.</param>
    /// <param name="category">Keep only tickets with this AI category: AccountAccess, Billing, TechnicalIssue, FeatureRequest, GeneralInquiry or Other.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <returns>The matching tickets; an empty array when nothing matches.</returns>
    /// <response code="200">The matching tickets.</response>
    /// <response code="400">A filter value is not one of the allowed names.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TicketResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, ProblemJson)]
    public async Task<ActionResult<IReadOnlyList<TicketResponse>>> ListAsync(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var filter = new TicketListFilter(
            ParseFilter<TicketStatus>(status, nameof(status), errors),
            ParseFilter<TicketPriority>(priority, nameof(priority), errors),
            ParseFilter<TicketCategory>(category, nameof(category), errors));
        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }

        var tickets = await _tickets.ListAsync(filter, cancellationToken);
        return Ok(tickets.Select(TicketResponse.FromTicket).ToList());
    }

    /// <summary>Gets one ticket with every field, including AI results and the latest draft reply (US1.3).</summary>
    /// <param name="id">The ticket id: a positive whole number.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <returns>The ticket.</returns>
    /// <response code="200">The ticket.</response>
    /// <response code="400">The id is not a positive whole number.</response>
    /// <response code="404">No ticket has this id.</response>
    [HttpGet("{id}", Name = GetTicketRouteName)]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, ProblemJson)]
    public async Task<ActionResult<TicketResponse>> GetAsync(string id, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetAsync(ParseId(id), cancellationToken);
        return Ok(TicketResponse.FromTicket(ticket));
    }

    /// <summary>Updates the title, description and priority of a ticket (US1.4).</summary>
    /// <remarks>
    /// All three fields are required. Status and AI fields cannot be changed here; if they are sent they are
    /// ignored. Use <c>PATCH /api/tickets/{id}/status</c> to change the status.
    /// </remarks>
    /// <param name="id">The ticket id: a positive whole number.</param>
    /// <param name="request">The new title, description and priority.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <returns>The updated ticket.</returns>
    /// <response code="200">The updated ticket.</response>
    /// <response code="400">The id is invalid, a field is missing or invalid, or the body is not valid JSON.</response>
    /// <response code="404">No ticket has this id.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, ProblemJson)]
    public async Task<ActionResult<TicketResponse>> UpdateAsync(
        string id,
        [FromBody] UpdateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var ticketId = ParseId(id);
        var ticket = await _tickets.UpdateAsync(
            ticketId,
            new UpdateTicketInput(request.Title, request.Description, request.Priority),
            cancellationToken);

        return Ok(TicketResponse.FromTicket(ticket));
    }

    /// <summary>Sets the status of a ticket; any status may follow any status (US1.5).</summary>
    /// <param name="id">The ticket id: a positive whole number.</param>
    /// <param name="request">The new status.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <returns>The updated ticket.</returns>
    /// <response code="200">The updated ticket.</response>
    /// <response code="400">The id is invalid, the status is missing or unknown, or the body is not valid JSON.</response>
    /// <response code="404">No ticket has this id.</response>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, ProblemJson)]
    public async Task<ActionResult<TicketResponse>> ChangeStatusAsync(
        string id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var ticketId = ParseId(id);
        var ticket = await _tickets.ChangeStatusAsync(ticketId, request.Status, cancellationToken);
        return Ok(TicketResponse.FromTicket(ticket));
    }

    /// <summary>
    /// Analyzes a ticket with AI and saves the result; the AI-suggested priority overwrites the ticket's priority (US2.1).
    /// </summary>
    /// <remarks>
    /// Works for a ticket in any status, and never changes the status. Only the title and description are sent to
    /// the AI. The validated category, sentiment, summary, suggested action and priority replace any earlier
    /// analysis; <c>previousPriority</c> shows the priority before it was overwritten. If the AI fails, times out
    /// or returns invalid output, nothing is saved. No request body.
    /// </remarks>
    /// <param name="id">The ticket id: a positive whole number.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <returns>The analysis, the previous priority and the updated ticket.</returns>
    /// <response code="200">The validated analysis, now saved on the ticket.</response>
    /// <response code="400">The id is not a positive whole number.</response>
    /// <response code="404">No ticket has this id; the AI was not called.</response>
    /// <response code="502">The AI provider failed or returned invalid output. Nothing was saved.</response>
    /// <response code="503">AI features are not configured on this server. Ticket operations still work.</response>
    /// <response code="504">The AI provider did not respond in time. Nothing was saved.</response>
    [HttpPost("{id}/analyze")]
    [ProducesResponseType(typeof(AnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway, ProblemJson)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable, ProblemJson)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout, ProblemJson)]
    public async Task<ActionResult<AnalysisResponse>> AnalyzeAsync(string id, CancellationToken cancellationToken)
    {
        var outcome = await _tickets.AnalyzeAsync(ParseId(id), cancellationToken);
        return Ok(AnalysisResponse.FromOutcome(outcome));
    }

    /// <summary>
    /// Generates an AI reply draft and stores it on the ticket for human review. The draft is never sent (US2.4).
    /// </summary>
    /// <remarks>
    /// Nothing is emailed, messaged or posted: the draft is only returned and saved as the ticket's latest draft,
    /// replacing any earlier one. Works for a ticket in any status, and never changes the status. When the ticket
    /// has been analyzed, its saved analysis is given to the AI as context. If the AI fails, times out or returns
    /// invalid output, nothing is saved. No request body.
    /// </remarks>
    /// <param name="id">The ticket id: a positive whole number.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <returns>The draft, its generation time and the "not sent" notice.</returns>
    /// <response code="200">The draft, saved on the ticket and not sent.</response>
    /// <response code="400">The id is not a positive whole number.</response>
    /// <response code="404">No ticket has this id; the AI was not called.</response>
    /// <response code="502">The AI provider failed or returned invalid output. Nothing was saved.</response>
    /// <response code="503">AI features are not configured on this server. Ticket operations still work.</response>
    /// <response code="504">The AI provider did not respond in time. Nothing was saved.</response>
    [HttpPost("{id}/draft-reply")]
    [ProducesResponseType(typeof(DraftReplyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway, ProblemJson)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable, ProblemJson)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout, ProblemJson)]
    public async Task<ActionResult<DraftReplyResponse>> DraftReplyAsync(string id, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.DraftReplyAsync(ParseId(id), cancellationToken);
        return Ok(DraftReplyResponse.FromTicket(ticket));
    }

    /// <summary>
    /// Parses the <c>{id}</c> route value. The route has no <c>:int</c> constraint on purpose, so that
    /// <c>abc</c>, <c>0</c> or <c>-1</c> give 400 naming <c>id</c> instead of a routing 404 (BR3.2).
    /// Only plain digits are accepted (no sign, spaces or decimal point).
    /// </summary>
    private static int ParseId(string id)
    {
        if (int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value >= 1)
        {
            return value;
        }

        throw new ValidationFailedException("id", "Id must be a positive whole number.");
    }

    /// <summary>
    /// Parses an optional query filter by enum name, ignoring case (BR2.2). Empty or whitespace means
    /// "no filter"; numbers and unknown names are recorded as an error listing the allowed values (BR2.3).
    /// </summary>
    private static TEnum? ParseFilter<TEnum>(string? value, string field, IDictionary<string, string[]> errors)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (StrictEnum.TryParse<TEnum>(value.Trim(), out var parsed))
        {
            return parsed;
        }

        var label = char.ToUpperInvariant(field[0]) + field[1..];
        errors[field] = [$"{label} is not valid. Allowed values: {StrictEnum.AllowedValues<TEnum>()}."];
        return null;
    }
}
