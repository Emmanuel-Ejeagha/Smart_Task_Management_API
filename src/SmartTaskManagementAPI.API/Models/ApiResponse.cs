using System;

namespace SmartTaskManagementAPI.API.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ApiResponse() { }
    public ApiResponse(T data, string message = "")
    {
        Success = true;
        Message = message;
        Data = data;
    }

    public static ApiResponse<T> SuccessResponse(T data, string message = "")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}

public class ApiResponse : ApiResponse<object>
{
    public ApiResponse() { }

    public ApiResponse(object data, string message = "") : base(data, message) { }

    public static new ApiResponse SuccessResponse(object data, string message = "")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Data = data
        };
    }
    
    public static new ApiResponse ErrorResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}