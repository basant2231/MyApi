namespace MyApi.Models;

/// <summary>
/// Consistent API response wrapper.
/// Every endpoint returns this shape — your Flutter app always knows what to expect.
/// 
/// Success: { "success": true, "message": "...", "data": {...}, "statusCode": 200 }
/// Error:   { "success": false, "message": "...", "errors": [...], "statusCode": 400 }
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public int StatusCode { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = 200
        };
    }

    public static ApiResponse<T> Created(T data, string message = "Created successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = 201
        };
    }

    public static ApiResponse<T> Fail(string message, int statusCode = 400, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            StatusCode = statusCode,
            Errors = errors
        };
    }
}

/// <summary>
/// Non-generic version for responses without data.
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string message = "Success")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            StatusCode = 200
        };
    }

    public new static ApiResponse Fail(string message, int statusCode = 400, List<string>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            StatusCode = statusCode,
            Errors = errors
        };
    }
}

