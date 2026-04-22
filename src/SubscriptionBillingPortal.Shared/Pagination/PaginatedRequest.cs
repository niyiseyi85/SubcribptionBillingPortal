namespace SubscriptionBillingPortal.Shared.Pagination;

/// <summary>
/// Base request model carrying pagination parameters.
/// All list queries must inherit or embed this.
/// </summary>
public sealed class PaginatedRequest
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private const int MinPageNumber = 1;

    private int _pageNumber = MinPageNumber;
    private int _pageSize = DefaultPageSize;

    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < MinPageNumber ? MinPageNumber : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : value < 1 ? DefaultPageSize : value;
    }
}
