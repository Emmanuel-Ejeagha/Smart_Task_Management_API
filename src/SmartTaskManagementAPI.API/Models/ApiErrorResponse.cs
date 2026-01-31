using System.Text.Json.Serialization;

namespace SmartTaskManagementAPI.API.Models;

public class ApiErrorResponse : ApiResponse
{
    [JsonPropertyName("errors")]
    public List<ApiError> Errors { get; set; } = new();
    
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    public static ApiErrorResponse ValidationError(List<ApiError> errors, string? requestId = null)
    {
        return new ApiErrorResponse
        {
            Success = false,
            Message = "Validation failed",
            Errors = errors,
            ErrorCode = "VALIDATION_ERROR",
            RequestId = requestId
        };
    }
    
    public static ApiErrorResponse NotFoundError(string message, string? requestId = null)
    {
        return new ApiErrorResponse
        {
            Success = false,
            Message = message,
            ErrorCode = "NOT_FOUND",
            RequestId = requestId
        };
    }
    
    public static ApiErrorResponse UnauthorizedError(string message, string? requestId = null)
    {
        return new ApiErrorResponse
        {
            Success = false,
            Message = message,
            ErrorCode = "UNAUTHORIZED",
            RequestId = requestId
        };
    }
    
    public static ApiErrorResponse ForbiddenError(string message, string? requestId = null)
    {
        return new ApiErrorResponse
        {
            Success = false,
            Message = message,
            ErrorCode = "FORBIDDEN",
            RequestId = requestId
        };
    }
    
    public static ApiErrorResponse InternalServerError(string message, string? requestId = null)
    {
        return new ApiErrorResponse
        {
            Success = false,
            Message = message,
            ErrorCode = "INTERNAL_SERVER_ERROR",
            RequestId = requestId
        };
    }
}

public class ApiError
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("code")]
    public string? Code { get; set; }
}