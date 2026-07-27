using MicroServiceSystem.SharedKernel.Guards;
using MicroServiceSystem.SharedKernel.Primitives;

namespace Coordinator.Domain.Aggregates;

public enum RegisterUserSagaState
{
    Started = 0,
    IdentityRegistered = 1,
    UserProfileCreated = 2,
    Compensating = 3,
    Completed = 4,
    Failed = 5
}

public sealed class RegisterUserSaga : TenantAggregateRoot<Guid>
{
    private RegisterUserSaga()
    {
    }

    private RegisterUserSaga(Guid id, string email, string userName, string displayName)
        : base(id)
    {
        Email = email;
        UserName = userName;
        DisplayName = displayName;
        State = RegisterUserSagaState.Started;
    }

    public string Email { get; private set; } = string.Empty;

    public string UserName { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public Guid? IdentityUserId { get; private set; }

    public Guid? UserProfileId { get; private set; }

    public RegisterUserSagaState State { get; private set; }

    public string? FailureReason { get; private set; }

    public static RegisterUserSaga Start(string email, string userName, string displayName)
    {
        Ensure.NotNullOrWhiteSpace(email);
        Ensure.NotNullOrWhiteSpace(userName);
        Ensure.NotNullOrWhiteSpace(displayName);
        Ensure.MaxLength(email, 256);
        Ensure.MaxLength(userName, 128);
        Ensure.MaxLength(displayName, 256);

        return new RegisterUserSaga(
            Guid.CreateVersion7(),
            email.Trim().ToLowerInvariant(),
            userName.Trim(),
            displayName.Trim());
    }

    public void MarkIdentityRegistered(Guid identityUserId)
    {
        Ensure.NotEmpty(identityUserId);
        IdentityUserId = identityUserId;
        State = RegisterUserSagaState.IdentityRegistered;
    }

    public void MarkUserProfileCreated(Guid userProfileId)
    {
        Ensure.NotEmpty(userProfileId);
        UserProfileId = userProfileId;
        State = RegisterUserSagaState.UserProfileCreated;
    }

    public void MarkCompensating(string reason)
    {
        Ensure.NotNullOrWhiteSpace(reason);
        FailureReason = reason;
        State = RegisterUserSagaState.Compensating;
    }

    public void MarkCompleted()
    {
        State = RegisterUserSagaState.Completed;
    }

    public void MarkFailed(string reason)
    {
        Ensure.NotNullOrWhiteSpace(reason);
        FailureReason = reason;
        State = RegisterUserSagaState.Failed;
    }
}
