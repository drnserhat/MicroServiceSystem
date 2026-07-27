using Coordinator.Application;
using Coordinator.Domain.Aggregates;
using MicroServiceSystem.BuildingBlocks.Saga;
using MicroServiceSystem.SharedKernel.Results;

namespace Coordinator.Application.Registration;

internal sealed class CreateUserProfileStep : ISagaStep<RegisterUserSagaContext>
{
    public string Name => "create-user-profile";

    public async Task<Result> ExecuteAsync(
        RegisterUserSagaContext context,
        CancellationToken cancellationToken = default)
    {
        Guid? identityUserId = context.Identity?.UserId ?? context.Saga.IdentityUserId;
        if (identityUserId is null)
        {
            context.Saga.MarkFailed("Identity registration result is missing.");
            await context.PersistAsync(cancellationToken);
            return Result.Failure(CoordinatorErrors.IdentityRegistrationFailed);
        }

        try
        {
            context.Profile = await context.UserClient.CreateProfileAsync(
                identityUserId.Value,
                context.Command.FirstName,
                context.Command.LastName,
                context.DisplayName,
                context.Command.TenantId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Persist Compensating before reverse steps run, so recovery can finish undo.
            context.Saga.MarkCompensating(ex.Message);
            await context.PersistAsync(cancellationToken);
            return Result.Failure(CoordinatorErrors.UserProfileCreationFailed);
        }

        context.Saga.MarkUserProfileCreated(context.Profile.Id);
        await context.PersistAsync(cancellationToken);
        return Result.Success();
    }

    public Task<Result> CompensateAsync(
        RegisterUserSagaContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());
}
