/** Mirrors SharedKernel FrameworkPermissions codes used by the admin SPA. */
export const FrameworkPermissions = {
  UsersProfilesRead: "users.profiles.read",
  UsersProfilesUpdate: "users.profiles.update",
  AuditEntriesRead: "audit.entries.read",
  AuditEntriesCreate: "audit.entries.create",
  NotificationMessagesCreate: "notification.messages.create",
  FileAssetsUpload: "file.assets.upload",
  LocationCountriesRead: "location.countries.read",
  LocationCountriesCreate: "location.countries.create",
  LocationCountriesWrite: "location.countries.write",
  SettingsValuesRead: "settings.values.read",
  SettingsValuesWrite: "settings.values.write",
  LoggingLogsRead: "logging.logs.read",
  RegistrationUsersCreate: "registration.users.create",
  IdentityTenantsRead: "identity.tenants.read",
  IdentityTenantsWrite: "identity.tenants.write",
  IdentityTenantDatabasesRead: "identity.tenant-databases.read",
  IdentityTenantDatabasesWrite: "identity.tenant-databases.write",
  IdentityUsersRead: "identity.users.read",
  IdentityUsersDisable: "identity.users.disable",
  IdentityRolesRead: "identity.roles.read",
  IdentityRolesAssign: "identity.roles.assign",
  IdentityRolesWrite: "identity.roles.write",
  OpsHealthRead: "ops.health.read",
  OpsOutboxRead: "ops.outbox.read",
  OpsOutboxWrite: "ops.outbox.write",
  OpsSagaRead: "ops.saga.read",
  OpsInboxRead: "ops.inbox.read",
  LoggingLogsIngest: "logging.logs.ingest",
} as const;

/** Allowlisted permission codes for custom role create/replace (mirrors FrameworkPermissions.KnownPermissionCodes). */
export const KnownPermissionCodes: string[] = Object.values(FrameworkPermissions);
