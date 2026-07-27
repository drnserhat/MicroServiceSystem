using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.SharedKernel.Models;

public sealed record ApiResponse<TData>
{
    private ApiResponse(bool succeeded, TData? data, ApiError? error, string? traceId)
    {
        Succeeded = succeeded;
        Data = data;
        Error = error;
        TraceId = traceId;
    }

    public bool Succeeded { get; }

    public TData? Data { get; }

    public ApiError? Error { get; }

    public string? TraceId { get; }

    public DateTimeOffset TimestampUtc { get; } = DateTimeOffset.UtcNow;

    public static ApiResponse<TData> Success(TData data, string? traceId = null) => new(true, data, null, traceId);

    public static ApiResponse<TData> Failure(Error error, string? traceId = null) =>
        new(false, default, ApiError.FromError(error), traceId);
}

public sealed record ApiError(string Code, string Description, IReadOnlyDictionary<string, string[]> Failures)
{
    public static ApiError FromError(Error error) => new(error.Code, error.Description, error.Failures);
}
