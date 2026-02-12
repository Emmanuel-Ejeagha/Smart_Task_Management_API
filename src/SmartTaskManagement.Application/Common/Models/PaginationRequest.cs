namespace SmartTaskManagement.Application.Common.Models;

/// <summary>
/// Request model for pagination
/// </summary>
public class PaginationRequest
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    private int _pageNumber = 1;
    private int _pageSize = DefaultPageSize;

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Page size (max 100)
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 1 : value);
    }

    /// <summary>
    /// Calculate skip for EF Core queries
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Create default pagination
    /// </summary>
    public static PaginationRequest Default => new();

    /// <summary>
    /// Create pagination with custom values
    /// </summary>
    public static PaginationRequest Create(int pageNumber = 1, int pageSize = DefaultPageSize)
    {
        return new PaginationRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}