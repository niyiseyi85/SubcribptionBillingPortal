namespace SubscriptionBillingPortal.Shared.Pagination;

/// <summary>
/// Generic paginated result returned by all list queries.
/// Carries metadata needed by consumers to implement pagination UI or further fetches.
/// </summary>
public sealed class PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static PaginatedResult<T> Create(IEnumerable<T> source, int totalCount, int pageNumber, int pageSize)
        => new()
        {
            Items = source.ToList().AsReadOnly(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
}
