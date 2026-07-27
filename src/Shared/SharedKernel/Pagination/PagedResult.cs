namespace MicroServiceSystem.SharedKernel.Pagination;

public sealed record PagedResult<T>
{
    private PagedResult(IReadOnlyCollection<T> items, long totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public IReadOnlyCollection<T> Items { get; }

    public long TotalCount { get; }

    public int PageNumber { get; }

    public int PageSize { get; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => PageNumber > PaginationDefaults.FirstPageNumber;

    public bool HasNextPage => PageNumber < TotalPages;

    public static PagedResult<T> Create(IReadOnlyCollection<T> items, long totalCount, PaginationRequest request) =>
        new(items, totalCount, request.NormalizedPageNumber, request.NormalizedPageSize);

    public static PagedResult<T> Create(IReadOnlyCollection<T> items, long totalCount, int pageNumber, int pageSize) =>
        new(items, totalCount, pageNumber, pageSize);

    public static PagedResult<T> Empty(PaginationRequest request) =>
        new([], 0, request.NormalizedPageNumber, request.NormalizedPageSize);

    public PagedResult<TTarget> Project<TTarget>(Func<T, TTarget> projection) =>
        new(Items.Select(projection).ToList(), TotalCount, PageNumber, PageSize);
}
