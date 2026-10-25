using FluentValidation;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Features.Tickets.Models;

namespace SupportDesk.Api.Features.Tickets.Validation;

/// <summary>Rules for creating a ticket (BR1.1, BR1.2): title 3-200 and description 10-4000 characters, trimmed.</summary>
public sealed class CreateTicketInputValidator : AbstractValidator<CreateTicketInput>
{
    /// <summary>Creates the validator.</summary>
    public CreateTicketInputValidator()
    {
        // Stop at the first failing check per field, but still check every field.
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Title)
            .TrimmedText("Title", TicketService.TitleMinLength, Ticket.TitleMaxLength)
            .OverridePropertyName("title");

        RuleFor(input => input.Description)
            .TrimmedText("Description", TicketService.DescriptionMinLength, Ticket.DescriptionMaxLength)
            .OverridePropertyName("description");
    }
}
