using MicroServiceSystem.BuildingBlocks.Authorization.Abstractions;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Authorization;

public sealed class ClaimsPermissionProvider(ICurrentUser currentUser) : IPermissionProvider
{
    public Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(currentUser.UserId == userId ? currentUser.Permissions : []);
}
