namespace MicroServiceSystem.SharedKernel.Constants;

/// <summary>
/// Canonical permission codes shared by Identity role seeding and API <c>[HasPermission]</c> attributes.
/// </summary>
public static class FrameworkPermissions
{
    public const string UsersProfilesRead = "users.profiles.read";

    public const string UsersProfilesUpdate = "users.profiles.update";

    public const string AuditEntriesRead = "audit.entries.read";
    public const string AuditEntriesCreate = "audit.entries.create";

    public const string NotificationMessagesCreate = "notification.messages.create";

    public const string FileAssetsUpload = "file.assets.upload";

    public const string LocationCountriesRead = "location.countries.read";
    public const string LocationCountriesCreate = "location.countries.create";
    public const string LocationCountriesWrite = "location.countries.write";

    public const string SettingsValuesRead = "settings.values.read";
    public const string SettingsValuesWrite = "settings.values.write";

    public const string LoggingLogsIngest = "logging.logs.ingest";
    public const string LoggingLogsRead = "logging.logs.read";

    /// <summary>
    /// Provision users through the RegisterUser saga. Not granted to ordinary members — self-signup
    /// is closed by default; tenant admins (or an internal bootstrap) hold this permission.
    /// </summary>
    public const string RegistrationUsersCreate = "registration.users.create";

    public const string MemberRoleName = "Member";

    public const string AdminRoleName = "Admin";

    /// <summary>
    /// Default permissions granted to every newly registered tenant member.
    /// </summary>
    public static IReadOnlyList<string> MemberDefaults { get; } =
    [
        UsersProfilesRead,
        UsersProfilesUpdate,
        AuditEntriesRead,
        AuditEntriesCreate,
        NotificationMessagesCreate,
        FileAssetsUpload,
        LocationCountriesRead,
        LocationCountriesCreate,
        LocationCountriesWrite,
        SettingsValuesRead,
        SettingsValuesWrite,
        LoggingLogsIngest,
        LoggingLogsRead
    ];

    /// <summary>
    /// Elevated permissions for tenant administrators. Includes member defaults plus user provisioning.
    /// </summary>
    public static IReadOnlyList<string> AdminDefaults { get; } =
    [
        .. MemberDefaults,
        RegistrationUsersCreate
    ];
}
