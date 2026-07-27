namespace MicroServiceSystem.SharedKernel.Constants;

public static class FrameworkHeaders
{
    public const string CorrelationId = "X-Correlation-Id";

    public const string TenantId = "X-Tenant-Id";

    public const string IdempotencyKey = "X-Idempotency-Key";

    public const string RequestId = "X-Request-Id";

    public const string ApiKey = "X-Api-Key";

    public const string ClientVersion = "X-Client-Version";
}
