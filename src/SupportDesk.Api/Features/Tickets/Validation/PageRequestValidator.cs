using FluentValidation;
using SupportDesk.Api.Features.Tickets.Models;

namespace SupportDesk.Api.Features.Tickets.Validation;

/// <summary>Rules for paging (FR3.3): page 1 or more, page size 1-200 (or a smaller cap). Missing values use the defaults.</summary>
public sealed class PageRequestValidator : AbstractValidator<PageRequest>
{
    /// <summary>Creates the validator.</summary>
    /// <param name="maxPageSize">The largest page size allowed; the MCP tool uses a smaller cap than REST.</param>
    public PageRequestValidator(int maxPageSize = PageRequest.MaxPageSize)
    {
        RuleFor(paging => paging.Page)
            .GreaterThanOrEqualTo(1)
            .When(paging => paging.Page is not null)
            .WithMessage("Page must be 1 or greater.")
            .OverridePropertyName("page");

        RuleFor(paging => paging.PageSize)
            .InclusiveBetween(1, maxPageSize)
            .When(paging => paging.PageSize is not null)
            .WithMessage($"Page size must be between 1 and {maxPageSize}.")
            .OverridePropertyName("pageSize");
    }
}
