using MicroServiceSystem.SharedKernel.Guards;
using MicroServiceSystem.SharedKernel.Primitives;

namespace MicroServiceSystem.Services.Identity.Domain.Aggregates;

/// <summary>
/// Platform tenant catalog entry. Not tenant-scoped itself — the catalog is global; rows are not
/// filtered by ambient tenant id.
/// </summary>
public sealed class Tenant : AuditableAggregateRoot<Guid>
{
    private Tenant()
    {
    }

    private Tenant(Guid id, string name, string slug)
        : base(id)
    {
        Name = name;
        Slug = slug;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static Tenant Provision(Guid id, string name, string slug)
    {
        Ensure.NotEmpty(id);
        Ensure.NotNullOrWhiteSpace(name);
        Ensure.NotNullOrWhiteSpace(slug);
        Ensure.MaxLength(name, TenantConstraints.NameMaxLength);
        Ensure.MaxLength(slug, TenantConstraints.SlugMaxLength);

        return new Tenant(
            id,
            name.Trim(),
            NormalizeSlug(slug));
    }

    public static Tenant Provision(string name, string slug) =>
        Provision(Guid.CreateVersion7(), name, slug);

    public void Rename(string name)
    {
        Ensure.NotNullOrWhiteSpace(name);
        Ensure.MaxLength(name, TenantConstraints.NameMaxLength);
        Name = name.Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public static string NormalizeSlug(string slug) =>
        slug.Trim().ToLowerInvariant();
}

public static class TenantConstraints
{
    public const int NameMaxLength = 128;

    public const int SlugMaxLength = 64;
}
