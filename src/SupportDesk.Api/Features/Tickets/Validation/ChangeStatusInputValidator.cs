using FluentValidation;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Features.Tickets.Models;

namespace SupportDesk.Api.Features.Tickets.Validation;

/// <summary>Rules for a status change (BR5.2): the status is required and must be Open, InProgress, Resolved or Closed.</summary>
public sealed class ChangeStatusInputValidator : AbstractValidator<ChangeStatusInput>
{
    /// <summary>Creates the validator.</summary>
    public ChangeStatusInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Status)
            .RequiredEnum<ChangeStatusInput, TicketStatus>("Status")
            .OverridePropertyName("status");
    }
}
