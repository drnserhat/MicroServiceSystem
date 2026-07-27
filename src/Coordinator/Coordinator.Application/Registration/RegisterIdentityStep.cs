using Coordinator.Domain.Aggregates;
using MicroServiceSystem.BuildingBlocks.Saga;
using MicroServiceSystem.SharedKernel.Results;

namespace Coordinator.Application.Registration;

internal sealed class RegisterIdentityStep : ISagaStep<RegisterUserSagaContext>
{
    public string Name => "register-identity";

    public async Task<Result> ExecuteAsync(
        RegisterUserSagaContext context,
        CancellationToken cancellationToken = default)
    {
        // Reserve the identity id and checkpoint it *before* the remote call. If the call succeeds but the
        // response is lost, the saga still holds the id, so the attempt can be retried against the same
        // user or undone. Without this a crash here leaves an identity nobody can find.
        if (context.Saga.IdentityUserId is null)
        {
            context.Saga.ReserveIdentityUserId(Guid.CreateVersion7());
            await context.PersistAsync(cancellationToken);
        }

        Guid identityUserId = context.Saga.IdentityUserId!.Value;

        try
        {
            context.Identity = await context.IdentityClient.RegisterAsync(
                identityUserId,
                context.Command.Email,
                context.Command.UserName,
                context.Command.Password,
                context.Command.TenantId,
                cancellationToken);
        }
        catch (Exception exception)
        {
            // The remote user may or may not exist. Undo against the reserved id, which is a no-op when
            // the call never landed.
            context.Saga.MarkCompensating(exception.Message);
            await context.PersistAsync(cancellationToken);

            Result compensation = await CompensateAsync(context, cancellationToken);

            return compensation.IsFailure
                ? compensation
                : Result.Failure(CoordinatorErrors.IdentityRegistrationFailed);
        }

        context.Saga.MarkIdentityRegistered(context.Identity.UserId);
        await context.PersistAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> CompensateAsync(
        RegisterUserSagaContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.Saga.IdentityUserId is not Guid identityUserId)
        {
            return Result.Success();
        }

        if (context.Saga.State != RegisterUserSagaState.Compensating)
        {
            context.Saga.MarkCompensating(context.Saga.FailureReason ?? "Compensating identity registration.");
            await context.PersistAsync(cancellationToken);
        }

        try
        {
            await context.IdentityClient.DisableAsync(
                identityUserId,
                $"Compensating registration saga {context.Saga.Id}: {context.Saga.FailureReason}",
                context.Command.TenantId,
                cancellationToken);
        }
        catch (Exception exception)
        {
            // Stay in Compensating so recovery retries the undo once this owner's lease lapses.
            context.Saga.MarkCompensating($"Identity compensation failed: {exception.Message}");
            await context.PersistAsync(cancellationToken);

            return Result.Failure(CoordinatorErrors.CompensationFailed);
        }

        context.Saga.MarkFailed(context.Saga.FailureReason ?? "Compensated identity registration.");
        await context.PersistAsync(cancellationToken);

        return Result.Success();
    }
}
