namespace SubscriptionBillingPortal.Shared.Responses;

/// <summary>
/// Unified API response envelope returned by every endpoint.
/// Guarantees a consistent contract for all consumers of this API.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public int StatusCode { get; init; }

    private ApiResponse() { }

    public static ApiResponse<T> Ok(T data, string message = "Request completed successfully.", int statusCode = 200)
        => new() { Success = true, Message = message, Data = data, StatusCode = statusCode };

    public static ApiResponse<T> Created(T data, string message = "Resource created successfully.")
        => new() { Success = true, Message = message, Data = data, StatusCode = 201 };

    public static ApiResponse<T> Fail(string message, int statusCode)
        => new() { Success = false, Message = message, Data = default, StatusCode = statusCode };
}
