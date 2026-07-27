namespace MicroServiceSystem.Contracts.Abstractions;

/// <summary>
/// Canonical service identifiers used for routing, telemetry resource names and event sources.
/// </summary>
public static class ServiceNames
{
    public const string Identity = "identity";

    public const string User = "user";

    public const string Location = "location";

    public const string Notification = "notification";

    public const string Logging = "logging";

    public const string Audit = "audit";

    public const string File = "file";

    public const string Settings = "settings";

    public const string Coordinator = "coordinator";

    public const string Gateway = "gateway";
}
