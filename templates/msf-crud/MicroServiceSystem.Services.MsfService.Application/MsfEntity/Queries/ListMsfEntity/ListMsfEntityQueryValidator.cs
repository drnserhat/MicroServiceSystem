using FluentValidation;
using MicroServiceSystem.SharedKernel.Pagination;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Queries.ListMsfEntity;

public sealed class ListMsfEntityQueryValidator : AbstractValidator<ListMsfEntityQuery>
{
    public ListMsfEntityQueryValidator()
    {
        RuleFor(query => query.Pagination.PageNumber)
            .GreaterThanOrEqualTo(PaginationDefaults.FirstPageNumber);

        RuleFor(query => query.Pagination.PageSize)
            .InclusiveBetween(1, PaginationDefaults.MaxPageSize);
    }
}
