namespace MicroServiceSystem.BuildingBlocks.Authorization.Abstractions;

/// <summary>
/// Supplies the effective permission set of the caller. The default implementation reads permission
/// claims from the access token; services owning richer models can resolve them from their store.
/// </summary>
public interface IPermissionProvider
{
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
