namespace SmartTaskManagement.Application.Common.Models;

/// <summary>
/// Request model for sorting
/// </summary>
public class SortingRequest
{
    /// <summary>
    /// Sort by field
    /// </summary>
    public string SortBy { get; set; } = string.Empty;

    /// <summary>
    /// Sort direction
    /// </summary>
    public SortDirection Direction { get; set; } = SortDirection.Ascending;

    /// <summary>
    /// Sort direction enum
    /// </summary>
    public enum SortDirection
    {
        Ascending,
        Descending
    }

    /// <summary>
    /// Create sorting request
    /// </summary>
    public static SortingRequest Create(string sortBy, SortDirection direction = SortDirection.Ascending)
    {
        return new SortingRequest
        {
            SortBy = sortBy,
            Direction = direction
        };
    }

    /// <summary>
    /// Check if sorting is specified
    /// </summary>
    public bool IsSpecified => !string.IsNullOrWhiteSpace(SortBy);
}