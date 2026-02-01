namespace SmartTaskManagementAPI.API.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<ApiError> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> SuccessResponse(T data, string message = "")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> ErrorResponse(string message, List<ApiError>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<ApiError>()
        };
    }
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse SuccessResponse(string message = "")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }

    public static ApiResponse ErrorResponse(string message, List<ApiError>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<ApiError>()
        };
    }
}
