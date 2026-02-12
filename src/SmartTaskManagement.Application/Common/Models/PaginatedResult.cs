namespace SmartTaskManagement.Application.Common.Models;

/// <summary>
/// Paginated result model
/// </summary>
/// <typeparam name="T">Item type</typeparam>
public class PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    private PaginatedResult(
        IReadOnlyList<T> items,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Create paginated result
    /// </summary>
    public static PaginatedResult<T> Create(
        IReadOnlyList<T> items,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        return new PaginatedResult<T>(items, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Create empty paginated result
    /// </summary>
    public static PaginatedResult<T> Empty(PaginationRequest pagination)
    {
        return new PaginatedResult<T>(
            new List<T>(),
            pagination.PageNumber,
            pagination.PageSize,
            0);
    }

    /// <summary>
    /// Map to different type using selector
    /// </summary>
    public PaginatedResult<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        var mappedItems = Items.Select(selector).ToList();
        return PaginatedResult<TResult>.Create(mappedItems, PageNumber, PageSize, TotalCount);
    }
}