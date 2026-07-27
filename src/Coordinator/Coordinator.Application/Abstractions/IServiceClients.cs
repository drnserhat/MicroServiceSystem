using Coordinator.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace Coordinator.Application.Abstractions;

public interface IRegisterUserSagaRepository : IRepository<RegisterUserSaga, Guid>
{
}

public interface IIdentityServiceClient
{
    Task<IdentityRegistrationResult> RegisterAsync(
        string email,
        string userName,
        string password,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task DisableAsync(Guid userId, string reason, Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record IdentityRegistrationResult(Guid UserId, string Email, string UserName);

public interface IUserServiceClient
{
    Task<UserProfileResult> CreateProfileAsync(
        Guid userId,
        string firstName,
        string lastName,
        string? displayName,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public sealed record UserProfileResult(
    Guid Id,
    string FirstName,
    string LastName,
    string DisplayName,
    bool IsActive);
