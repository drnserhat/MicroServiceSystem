using System.ComponentModel.DataAnnotations;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Configuration;

public sealed class MongoOptions
{
    public const string SectionName = "Persistence:Mongo";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string DatabaseName { get; set; } = string.Empty;

    public int CommandTimeoutSeconds { get; set; } = 30;

    public bool UseTransactions { get; set; }
}
