namespace MicroServiceSystem.SharedKernel.Constants;

/// <summary>
/// Canonical permission codes shared by Identity role seeding and API <c>[HasPermission]</c> attributes.
/// </summary>
public static class FrameworkPermissions
{
    public const string UsersProfilesRead = "users.profiles.read";

    public const string AuditEntriesRead = "audit.entries.read";
    public const string AuditEntriesCreate = "audit.entries.create";

    public const string NotificationMessagesCreate = "notification.messages.create";

    public const string FileAssetsUpload = "file.assets.upload";

    public const string LocationCountriesRead = "location.countries.read";
    public const string LocationCountriesCreate = "location.countries.create";

    public const string SettingsValuesRead = "settings.values.read";
    public const string SettingsValuesWrite = "settings.values.write";

    public const string LoggingLogsIngest = "logging.logs.ingest";
    public const string LoggingLogsRead = "logging.logs.read";

    public const string MemberRoleName = "Member";

    /// <summary>
    /// Default permissions granted to every newly registered tenant member.
    /// </summary>
    public static IReadOnlyList<string> MemberDefaults { get; } =
    [
        UsersProfilesRead,
        AuditEntriesRead,
        AuditEntriesCreate,
        NotificationMessagesCreate,
        FileAssetsUpload,
        LocationCountriesRead,
        LocationCountriesCreate,
        SettingsValuesRead,
        SettingsValuesWrite,
        LoggingLogsIngest,
        LoggingLogsRead
    ];
}
