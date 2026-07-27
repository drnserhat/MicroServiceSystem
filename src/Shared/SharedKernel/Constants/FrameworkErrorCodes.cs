namespace MicroServiceSystem.SharedKernel.Constants;

public static class FrameworkErrorCodes
{
    public const string Validation = "general.validation";

    public const string NotFound = "general.not_found";

    public const string Conflict = "general.conflict";

    public const string Unauthorized = "general.unauthorized";

    public const string Forbidden = "general.forbidden";

    public const string Unexpected = "general.unexpected";

    public const string Concurrency = "general.concurrency";

    public const string TenantMissing = "general.tenant_missing";

    public const string DependencyUnavailable = "general.dependency_unavailable";

    public const string TooManyRequests = "general.too_many_requests";
}
