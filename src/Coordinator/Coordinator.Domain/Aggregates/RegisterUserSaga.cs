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

public sealed class RegisterUserSaga : SagaAggregateRoot<RegisterUserSagaState>
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
        TransitionTo(RegisterUserSagaState.Started);
    }

    public string Email { get; private set; } = string.Empty;

    public string UserName { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public Guid? IdentityUserId { get; private set; }

    public Guid? UserProfileId { get; private set; }

    public override bool IsTerminal =>
        State is RegisterUserSagaState.Completed or RegisterUserSagaState.Failed;

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

    /// <summary>
    /// Records the id the remote identity will be created with, before the call is made. Without this the
    /// saga cannot tell an untried registration apart from one whose response was lost, and recovery has
    /// no handle to undo the user that may already exist.
    /// </summary>
    public void ReserveIdentityUserId(Guid identityUserId)
    {
        Ensure.NotEmpty(identityUserId);
        IdentityUserId = identityUserId;
    }

    public void MarkIdentityRegistered(Guid identityUserId)
    {
        Ensure.NotEmpty(identityUserId);
        IdentityUserId = identityUserId;
        TransitionTo(RegisterUserSagaState.IdentityRegistered);
    }

    public void MarkUserProfileCreated(Guid userProfileId)
    {
        Ensure.NotEmpty(userProfileId);
        UserProfileId = userProfileId;
        TransitionTo(RegisterUserSagaState.UserProfileCreated);
    }

    public void MarkCompensating(string reason) =>
        BeginCompensation(RegisterUserSagaState.Compensating, reason);

    public void MarkCompleted() => Complete(RegisterUserSagaState.Completed);

    public void MarkFailed(string reason) => Fail(RegisterUserSagaState.Failed, reason);
}
