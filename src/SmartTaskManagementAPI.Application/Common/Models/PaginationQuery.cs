using System;

namespace SmartTaskManagementAPI.Application.Common.Models;

public class PaginationQuery
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
    }
    public string? SortBy { get; set; }
    public bool IsDescending { get; set; } = false;
    public string? SearchTerm { get; set; }
}
