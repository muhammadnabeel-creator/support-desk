using FluentValidation;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Features.Tickets.Models;

namespace SupportDesk.Api.Features.Tickets.Validation;

/// <summary>Rules for the list filters (BR2.3): each filter is optional but must be a defined value when present.</summary>
public sealed class TicketListFilterValidator : AbstractValidator<TicketListFilter>
{
    /// <summary>Creates the validator.</summary>
    public TicketListFilterValidator()
    {
        RuleFor(filter => filter.Status)
            .OptionalEnum<TicketListFilter, TicketStatus>("Status")
            .OverridePropertyName("status");

        RuleFor(filter => filter.Priority)
            .OptionalEnum<TicketListFilter, TicketPriority>("Priority")
            .OverridePropertyName("priority");

        RuleFor(filter => filter.Category)
            .OptionalEnum<TicketListFilter, TicketCategory>("Category")
            .OverridePropertyName("category");
    }
}
