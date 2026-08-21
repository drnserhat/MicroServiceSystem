using MicroServiceSystem.SharedKernel.Guards;
using MicroServiceSystem.SharedKernel.Primitives;

namespace MicroServiceSystem.Services.Identity.Domain.Aggregates;

/// <summary>
/// Postgres fleet member. Catalog-only; lives in Identity shared DB.
/// </summary>
public sealed class PostgresCluster : AuditableAggregateRoot<Guid>
{
    private PostgresCluster()
    {
    }

    private PostgresCluster(
        Guid id,
        string name,
        string slug,
        string host,
        int port,
        string adminSecretRef,
        int? maxDatabases,
        bool isDefault)
        : base(id)
    {
        Name = name;
        Slug = slug;
        Host = host;
        Port = port;
        AdminSecretRef = adminSecretRef;
        MaxDatabases = maxDatabases;
        IsDefault = isDefault;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string Host { get; private set; } = string.Empty;

    public int Port { get; private set; }

    /// <summary>Configuration key for the provisioner admin connection string (never a password literal).</summary>
    public string AdminSecretRef { get; private set; } = string.Empty;

    public int? MaxDatabases { get; private set; }

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public static PostgresCluster Create(
        string name,
        string slug,
        string host,
        int port,
        string adminSecretRef,
        int? maxDatabases = null,
        bool isDefault = false,
        Guid? id = null)
    {
        Ensure.NotNullOrWhiteSpace(name);
        Ensure.NotNullOrWhiteSpace(slug);
        Ensure.NotNullOrWhiteSpace(host);
        Ensure.NotNullOrWhiteSpace(adminSecretRef);
        Ensure.MaxLength(name, PostgresClusterConstraints.NameMaxLength);
        Ensure.MaxLength(slug, PostgresClusterConstraints.SlugMaxLength);
        Ensure.MaxLength(host, PostgresClusterConstraints.HostMaxLength);
        Ensure.MaxLength(adminSecretRef, PostgresClusterConstraints.SecretRefMaxLength);

        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port));
        }

        return new PostgresCluster(
            id ?? Guid.CreateVersion7(),
            name.Trim(),
            NormalizeSlug(slug),
            host.Trim(),
            port,
            adminSecretRef.Trim(),
            maxDatabases,
            isDefault);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public static string NormalizeSlug(string slug) => slug.Trim().ToLowerInvariant();
}

public static class PostgresClusterConstraints
{
    public const int NameMaxLength = 128;

    public const int SlugMaxLength = 64;

    public const int HostMaxLength = 256;

    public const int SecretRefMaxLength = 256;
}
