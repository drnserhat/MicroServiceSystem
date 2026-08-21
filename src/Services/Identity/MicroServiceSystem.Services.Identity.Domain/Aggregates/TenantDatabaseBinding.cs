using MicroServiceSystem.SharedKernel.Guards;
using MicroServiceSystem.SharedKernel.Primitives;

namespace MicroServiceSystem.Services.Identity.Domain.Aggregates;

public enum TenantDatabaseStatus
{
    Provisioning = 0,
    Ready = 1,
    Migrating = 2,
    Degraded = 3,
    Disabled = 4,
    Failed = 5
}

/// <summary>
/// Maps a tenant + service to a physical database. Catalog-only; no passwords.
/// </summary>
public sealed class TenantDatabaseBinding : AuditableAggregateRoot<Guid>
{
    private TenantDatabaseBinding()
    {
    }

    private TenantDatabaseBinding(
        Guid id,
        Guid tenantId,
        string serviceKey,
        Guid clusterId,
        string databaseName,
        string username,
        string secretRef)
        : base(id)
    {
        TenantId = tenantId;
        ServiceKey = serviceKey;
        ClusterId = clusterId;
        DatabaseName = databaseName;
        Username = username;
        SecretRef = secretRef;
        Status = TenantDatabaseStatus.Provisioning;
        SchemaVersion = null;
        LastError = null;
    }

    public Guid TenantId { get; private set; }

    public string ServiceKey { get; private set; } = string.Empty;

    public Guid ClusterId { get; private set; }

    public string DatabaseName { get; private set; } = string.Empty;

    public string Username { get; private set; } = string.Empty;

    /// <summary>Configuration key for the app-role password (resolved by the owning service).</summary>
    public string SecretRef { get; private set; } = string.Empty;

    public TenantDatabaseStatus Status { get; private set; }

    public string? SchemaVersion { get; private set; }

    public string? LastError { get; private set; }

    public static TenantDatabaseBinding StartProvision(
        Guid tenantId,
        string serviceKey,
        Guid clusterId,
        string databaseName,
        string username,
        string secretRef,
        Guid? id = null)
    {
        Ensure.NotEmpty(tenantId);
        Ensure.NotEmpty(clusterId);
        Ensure.NotNullOrWhiteSpace(serviceKey);
        Ensure.NotNullOrWhiteSpace(databaseName);
        Ensure.NotNullOrWhiteSpace(username);
        Ensure.NotNullOrWhiteSpace(secretRef);
        Ensure.MaxLength(serviceKey, TenantDatabaseBindingConstraints.ServiceKeyMaxLength);
        Ensure.MaxLength(databaseName, TenantDatabaseBindingConstraints.DatabaseNameMaxLength);
        Ensure.MaxLength(username, TenantDatabaseBindingConstraints.UsernameMaxLength);
        Ensure.MaxLength(secretRef, TenantDatabaseBindingConstraints.SecretRefMaxLength);

        if (!KnownServiceKeys.IsAllowed(serviceKey))
        {
            throw new ArgumentException($"Service key '{serviceKey}' is not allow-listed.", nameof(serviceKey));
        }

        return new TenantDatabaseBinding(
            id ?? Guid.CreateVersion7(),
            tenantId,
            KnownServiceKeys.Normalize(serviceKey),
            clusterId,
            databaseName.Trim().ToLowerInvariant(),
            username.Trim(),
            secretRef.Trim());
    }

    public void MarkMigrating()
    {
        Status = TenantDatabaseStatus.Migrating;
        LastError = null;
    }

    public void MarkReady(string? schemaVersion = null)
    {
        Status = TenantDatabaseStatus.Ready;
        SchemaVersion = schemaVersion;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Ensure.NotNullOrWhiteSpace(error);
        Status = TenantDatabaseStatus.Failed;
        LastError = TruncateError(error);
    }

    public void MarkDegraded(string? error = null)
    {
        Status = TenantDatabaseStatus.Degraded;
        if (!string.IsNullOrWhiteSpace(error))
        {
            LastError = TruncateError(error);
        }
    }

    public void Disable()
    {
        Status = TenantDatabaseStatus.Disabled;
    }

    public void RestartProvision()
    {
        Status = TenantDatabaseStatus.Provisioning;
        LastError = null;
        SchemaVersion = null;
    }

    private static string TruncateError(string error)
    {
        string trimmed = error.Trim();
        return trimmed.Length <= TenantDatabaseBindingConstraints.LastErrorMaxLength
            ? trimmed
            : trimmed[..TenantDatabaseBindingConstraints.LastErrorMaxLength];
    }
}

public static class TenantDatabaseBindingConstraints
{
    public const int ServiceKeyMaxLength = 64;

    public const int DatabaseNameMaxLength = 63;

    public const int UsernameMaxLength = 128;

    public const int SecretRefMaxLength = 256;

    public const int LastErrorMaxLength = 1024;
}

/// <summary>Phase-1 allow-list for branch data-plane services.</summary>
public static class KnownServiceKeys
{
    public const string User = "user";

    public static bool IsAllowed(string serviceKey) =>
        string.Equals(Normalize(serviceKey), User, StringComparison.Ordinal);

    public static string Normalize(string serviceKey) => serviceKey.Trim().ToLowerInvariant();
}
