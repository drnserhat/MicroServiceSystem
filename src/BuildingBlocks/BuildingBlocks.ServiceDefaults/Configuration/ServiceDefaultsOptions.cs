namespace MicroServiceSystem.BuildingBlocks.ServiceDefaults.Configuration;

public sealed class ServiceDefaultsOptions
{
    public const string SectionName = "ServiceDefaults";

    public string ServiceName { get; set; } = string.Empty;

    public string ServiceDescription { get; set; } = string.Empty;

    public bool EnableSwagger { get; set; } = true;

    public bool EnableResponseCompression { get; set; } = true;

    public bool EnableRateLimiting { get; set; } = true;

    public bool EnableLocalization { get; set; } = true;

    public bool EnableIdempotency { get; set; } = true;

    public string DefaultApiVersion { get; set; } = "1.0";

    public CorsPolicyOptions Cors { get; set; } = new();

    public RateLimitingOptions RateLimiting { get; set; } = new();

    public SecurityHeaderOptions SecurityHeaders { get; set; } = new();
}

public sealed class CorsPolicyOptions
{
    public const string PolicyName = "FrameworkDefaultCors";

    public string[] AllowedOrigins { get; set; } = [];

    public string[] AllowedMethods { get; set; } = [];

    public string[] AllowedHeaders { get; set; } = [];

    public bool AllowCredentials { get; set; }
}

public sealed class RateLimitingOptions
{
    public const string GlobalPolicyName = "FrameworkGlobalRateLimit";

    public int PermitLimit { get; set; } = 100;

    public int WindowSeconds { get; set; } = 60;

    public int QueueLimit { get; set; }

    public int SegmentsPerWindow { get; set; } = 6;
}

public sealed class SecurityHeaderOptions
{
    public bool Enabled { get; set; } = true;

    public string ContentSecurityPolicy { get; set; } = "default-src 'self'";

    public string ReferrerPolicy { get; set; } = "no-referrer";

    public string PermissionsPolicy { get; set; } = "geolocation=(), microphone=(), camera=()";

    public int StrictTransportSecurityMaxAgeDays { get; set; } = 365;
}
