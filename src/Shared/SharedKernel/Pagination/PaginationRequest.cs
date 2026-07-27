namespace MicroServiceSystem.SharedKernel.Pagination;

public sealed record PaginationRequest
{
    public int PageNumber { get; init; } = PaginationDefaults.FirstPageNumber;

    public int PageSize { get; init; } = PaginationDefaults.DefaultPageSize;

    public string? SortBy { get; init; }

    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;

    public string? Search { get; init; }

    public int NormalizedPageNumber => PageNumber < PaginationDefaults.FirstPageNumber
        ? PaginationDefaults.FirstPageNumber
        : PageNumber;

    public int NormalizedPageSize => PageSize switch
    {
        < 1 => PaginationDefaults.DefaultPageSize,
        > PaginationDefaults.MaxPageSize => PaginationDefaults.MaxPageSize,
        _ => PageSize
    };

    public int Skip => (NormalizedPageNumber - PaginationDefaults.FirstPageNumber) * NormalizedPageSize;

    public int Take => NormalizedPageSize;
}
