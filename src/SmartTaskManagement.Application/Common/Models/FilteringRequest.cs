namespace SmartTaskManagement.Application.Common.Models;

/// <summary>
/// Request model for filtering
/// </summary>
public class FilteringRequest
{
    /// <summary>
    /// Filter by field
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Filter operator
    /// </summary>
    public FilterOperator Operator { get; set; } = FilterOperator.Equals;

    /// <summary>
    /// Filter value
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Filter operators
    /// </summary>
    public enum FilterOperator
    {
        Equals,
        Contains,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        StartsWith,
        EndsWith,
        NotEquals
    }

    /// <summary>
    /// Create filtering request
    /// </summary>
    public static FilteringRequest Create(string field, FilterOperator op, string value)
    {
        return new FilteringRequest
        {
            Field = field,
            Operator = op,
            Value = value
        };
    }

    /// <summary>
    /// Check if filtering is specified
    /// </summary>
    public bool IsSpecified => !string.IsNullOrWhiteSpace(Field) && !string.IsNullOrWhiteSpace(Value);
}