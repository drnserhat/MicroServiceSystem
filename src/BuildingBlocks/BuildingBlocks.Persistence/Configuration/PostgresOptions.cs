using System.ComponentModel.DataAnnotations;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Configuration;

public sealed class PostgresOptions
{
    public const string SectionName = "Persistence:Postgres";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    public string Schema { get; set; } = "public";

    public int CommandTimeoutSeconds { get; set; } = 30;

    public int MaxRetryCount { get; set; } = 5;

    public int MaxRetryDelaySeconds { get; set; } = 10;

    public bool EnableSensitiveDataLogging { get; set; }

    public bool EnableDetailedErrors { get; set; }

    public bool ApplyMigrationsOnStartup { get; set; }
}
