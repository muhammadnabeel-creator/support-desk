using FluentValidation;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Features.Tickets.Models;

namespace SupportDesk.Api.Features.Tickets.Validation;

/// <summary>Rules for updating a ticket (BR4.1): the create rules plus a required, defined priority.</summary>
public sealed class UpdateTicketInputValidator : AbstractValidator<UpdateTicketInput>
{
    /// <summary>Creates the validator.</summary>
    public UpdateTicketInputValidator()
    {
        // Stop at the first failing check per field, but still check every field.
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Title)
            .TrimmedText("Title", TicketService.TitleMinLength, Ticket.TitleMaxLength)
            .OverridePropertyName("title");

        RuleFor(input => input.Description)
            .TrimmedText("Description", TicketService.DescriptionMinLength, Ticket.DescriptionMaxLength)
            .OverridePropertyName("description");

        RuleFor(input => input.Priority)
            .RequiredEnum<UpdateTicketInput, TicketPriority>("Priority")
            .OverridePropertyName("priority");
    }
}
