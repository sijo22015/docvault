namespace DocVault.Application.DTOs.Common;

public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Error,
    string? TraceId
)
{
    public static ApiResponse<T> Ok(T data, string? traceId = null) => new(true, data, null, traceId);
    public static ApiResponse<T> Fail(string error, string? traceId = null) => new(false, default, error, traceId);
}
