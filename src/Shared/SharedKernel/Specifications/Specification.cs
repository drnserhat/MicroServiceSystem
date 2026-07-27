using System.Linq.Expressions;
using MicroServiceSystem.SharedKernel.Pagination;

namespace MicroServiceSystem.SharedKernel.Specifications;

public abstract class Specification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = [];
    private readonly List<string> _includeStrings = [];
    private readonly List<SpecificationOrder<T>> _orderExpressions = [];
    private Func<T, bool>? _compiledCriteria;

    protected Specification()
    {
    }

    protected Specification(Expression<Func<T, bool>> criteria) => Criteria = criteria;

    public Expression<Func<T, bool>>? Criteria { get; private set; }

    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes;

    public IReadOnlyList<string> IncludeStrings => _includeStrings;

    public IReadOnlyList<SpecificationOrder<T>> OrderExpressions => _orderExpressions;

    public int? Skip { get; private set; }

    public int? Take { get; private set; }

    public bool IsPagingEnabled => Skip.HasValue || Take.HasValue;

    public bool AsNoTracking { get; private set; }

    public bool AsSplitQuery { get; private set; }

    public bool IgnoreQueryFilters { get; private set; }

    public bool IsSatisfiedBy(T candidate)
    {
        if (Criteria is null)
        {
            return true;
        }

        _compiledCriteria ??= Criteria.Compile();
        return _compiledCriteria(candidate);
    }

    protected void Where(Expression<Func<T, bool>> criteria)
    {
        Criteria = Criteria is null ? criteria : Criteria.AndAlso(criteria);
        _compiledCriteria = null;
    }

    protected void AddInclude(Expression<Func<T, object>> includeExpression) => _includes.Add(includeExpression);

    protected void AddInclude(string includeExpression) => _includeStrings.Add(includeExpression);

    protected void OrderBy(Expression<Func<T, object>> keySelector) =>
        _orderExpressions.Add(new SpecificationOrder<T>(keySelector, SortDirection.Ascending));

    protected void OrderByDescending(Expression<Func<T, object>> keySelector) =>
        _orderExpressions.Add(new SpecificationOrder<T>(keySelector, SortDirection.Descending));

    protected void ApplyPaging(PaginationRequest request)
    {
        Skip = request.Skip;
        Take = request.Take;
    }

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    protected void ApplyNoTracking() => AsNoTracking = true;

    protected void ApplySplitQuery() => AsSplitQuery = true;

    protected void ApplyIgnoreQueryFilters() => IgnoreQueryFilters = true;
}
