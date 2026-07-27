using System.Linq.Expressions;
using MicroServiceSystem.SharedKernel.Pagination;

namespace MicroServiceSystem.SharedKernel.Specifications;

public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }

    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    IReadOnlyList<string> IncludeStrings { get; }

    IReadOnlyList<SpecificationOrder<T>> OrderExpressions { get; }

    int? Skip { get; }

    int? Take { get; }

    bool IsPagingEnabled { get; }

    bool AsNoTracking { get; }

    bool AsSplitQuery { get; }

    bool IgnoreQueryFilters { get; }

    bool IsSatisfiedBy(T candidate);
}

public sealed record SpecificationOrder<T>(Expression<Func<T, object>> KeySelector, SortDirection Direction);
