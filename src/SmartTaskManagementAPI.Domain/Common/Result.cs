
namespace SmartTaskManagementAPI.Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess!;
    public string Error { get; }
    public string? ErrorCode { get; }

   protected Result(bool isSuccess, string error, string? errorCode = null)
    {
        if (isSuccess && !string.IsNullOrWhiteSpace(error))
            throw new InvalidOperationException("Successful result cannot have an error");
        
        if (!isSuccess && string.IsNullOrWhiteSpace(error))
            throw new InvalidOperationException("Failed result must have an error");

        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);

    public static Result Failure(string error, string? errorCode = null) => new(false, error, errorCode);
    public static Result ValidationFailure(string error) => new(false, error, "VALIDATION_ERROR");
}

public class Result<T> : Result
{
    private readonly T _value;

    public T Value
    {
        get
        {
            if (!IsSuccess)
                throw new InvalidOperationException("Cannot access value of a failed result");
            return _value;
        }
    }

    protected internal Result(T value, bool isSuccess, string error, string? errorCode = null) : base(isSuccess, error, errorCode)
    {
        _value = value;
    }
}
