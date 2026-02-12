namespace SmartTaskManagement.Application.Common.Models;

/// <summary>
/// Result pattern for operation results with error handling
/// Inspired by ErrorOr library pattern
/// </summary>
public class Result<T>
{
    public T? Value { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; } = string.Empty;
    public List<ValidationError> ValidationErrors { get; } = new();

    private Result(T value)
    {
        Value = value;
        IsSuccess = true;
    }

    private Result(string error)
    {
        Error = error;
        IsSuccess = false;
    }

    private Result(List<ValidationError> validationErrors)
    {
        ValidationErrors = validationErrors;
        IsSuccess = false;
        Error = "Validation failed";
    }

    /// <summary>
    /// Create success result
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Create failure result
    /// </summary>
    public static Result<T> Failure(string error) => new(error);

    /// <summary>
    /// Create validation failure result
    /// </summary>
    public static Result<T> ValidationFailure(List<ValidationError> errors) => new(errors);

    /// <summary>
    /// Implicit conversion to bool
    /// </summary>
    public static implicit operator bool(Result<T> result) => result.IsSuccess;

    /// <summary>
    /// Implicit conversion from value
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);
}

/// <summary>
/// Validation error model
/// </summary>
public class ValidationError
{
    public string PropertyName { get; }
    public string ErrorMessage { get; }
    public string ErrorCode { get; }

    public ValidationError(string propertyName, string errorMessage, string errorCode = "ValidationError")
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Non-generic Result for operations without return value
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; } = string.Empty;
    public List<ValidationError> ValidationErrors { get; } = new();

    private Result(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    private Result(string error)
    {
        Error = error;
        IsSuccess = false;
    }

    private Result(List<ValidationError> validationErrors)
    {
        ValidationErrors = validationErrors;
        IsSuccess = false;
        Error = "Validation failed";
    }

    /// <summary>
    /// Create success result
    /// </summary>
    public static Result Success() => new(true);

    /// <summary>
    /// Create failure result
    /// </summary>
    public static Result Failure(string error) => new(error);

    /// <summary>
    /// Create validation failure result
    /// </summary>
    public static Result ValidationFailure(List<ValidationError> errors) => new(errors);
}