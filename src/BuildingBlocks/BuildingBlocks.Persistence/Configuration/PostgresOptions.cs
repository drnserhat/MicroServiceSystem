using System.ComponentModel.DataAnnotations;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Configuration;

public sealed class PostgresOptions
{
    public const string SectionName = "Persistence:Postgres";

    public const string ModeShared = "Shared";

    public const string ModeTenantScoped = "TenantScoped";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// <see cref="ModeShared"/> uses <see cref="ConnectionString"/> for every request.
    /// <see cref="ModeTenantScoped"/> resolves per ambient tenant via <c>ITenantConnectionStringProvider</c>.
    /// </summary>
    public string Mode { get; set; } = ModeShared;

    /// <summary>Service key used when <see cref="Mode"/> is tenant-scoped (e.g. user).</summary>
    public string ServiceKey { get; set; } = string.Empty;

    public string Schema { get; set; } = "public";

    public int CommandTimeoutSeconds { get; set; } = 30;

    public int MaxRetryCount { get; set; } = 5;

    public int MaxRetryDelaySeconds { get; set; } = 10;

    public bool EnableSensitiveDataLogging { get; set; }

    public bool EnableDetailedErrors { get; set; }

    public bool ApplyMigrationsOnStartup { get; set; }
}
