using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.SharedKernel.Pagination;
using MicroServiceSystem.SharedKernel.Specifications;

namespace MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;

/// <summary>
/// Translates a specification into an EF Core query. Keeping the translation here is what lets the
/// application layer describe queries without referencing EF.
/// </summary>
public static class SpecificationEvaluator
{
    public static IQueryable<T> Apply<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);

        if (specification.IgnoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        if (specification.OrderExpressions.Count > 0)
        {
            SpecificationOrder<T> first = specification.OrderExpressions[0];

            IOrderedQueryable<T> ordered = first.Direction is SortDirection.Descending
                ? query.OrderByDescending(first.KeySelector)
                : query.OrderBy(first.KeySelector);

            foreach (SpecificationOrder<T> order in specification.OrderExpressions.Skip(1))
            {
                ordered = order.Direction is SortDirection.Descending
                    ? ordered.ThenByDescending(order.KeySelector)
                    : ordered.ThenBy(order.KeySelector);
            }

            query = ordered;
        }

        if (specification.AsSplitQuery)
        {
            query = query.AsSplitQuery();
        }

        if (specification.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query;
    }

    /// <summary>
    /// Paging is applied separately from the rest of the specification so the total count can be taken
    /// from the unpaged query.
    /// </summary>
    public static IQueryable<T> ApplyPaging<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        if (!specification.IsPagingEnabled)
        {
            return query;
        }

        if (specification.Skip is { } skip)
        {
            query = query.Skip(skip);
        }

        return specification.Take is { } take ? query.Take(take) : query;
    }
}
