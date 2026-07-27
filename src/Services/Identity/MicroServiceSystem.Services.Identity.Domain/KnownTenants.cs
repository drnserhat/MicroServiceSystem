namespace MicroServiceSystem.Services.Identity.Domain;

/// <summary>
/// Well-known tenant ids used by local demos and README samples. Production tenants are provisioned
/// through the catalog API; these constants must never be treated as an allow-list by themselves.
/// </summary>
public static class KnownTenants
{
    public static readonly Guid DevelopmentDemo = Guid.Parse("11111111-1111-1111-1111-111111111111");
}
