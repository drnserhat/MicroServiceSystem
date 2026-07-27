using MicroServiceSystem.Services.MsfService.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Pagination;
using MicroServiceSystem.SharedKernel.Specifications;

namespace MicroServiceSystem.Services.MsfService.Domain.Specifications;

public sealed class MsfEntityByIdSpecification : Specification<MsfEntity>
{
    public MsfEntityByIdSpecification(Guid id)
        : base(entity => entity.Id == id)
    {
    }
}

public sealed class MsfEntitySearchSpecification : Specification<MsfEntity>
{
    public MsfEntitySearchSpecification(PaginationRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string search = request.Search.Trim();
            Where(entity => entity.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (request.SortDirection == SortDirection.Descending)
        {
            OrderByDescending(entity => entity.Name);
        }
        else
        {
            OrderBy(entity => entity.Name);
        }

        ApplyNoTracking();
    }
}
