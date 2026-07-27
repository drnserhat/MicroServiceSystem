namespace MicroServiceSystem.BuildingBlocks.Logging.Configuration;

public sealed class FrameworkLoggingOptions
{
    public const string SectionName = "Logging:Framework";

    public string MinimumLevel { get; set; } = "Information";

    public bool WriteToConsole { get; set; } = true;

    public string SeqServerUrl { get; set; } = string.Empty;

    public string SeqApiKey { get; set; } = string.Empty;

    public string MongoConnectionString { get; set; } = string.Empty;

    public string MongoCollectionName { get; set; } = "system_logs";

    public bool LogRequestBody { get; set; }

    public bool LogResponseBody { get; set; }

    public string[] SensitivePropertyNames { get; set; } =
    [
        "password",
        "newPassword",
        "currentPassword",
        "token",
        "refreshToken",
        "accessToken",
        "authorization",
        "secret",
        "apiKey"
    ];
}
