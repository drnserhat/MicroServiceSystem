using Coordinator.Application.Abstractions;
using Coordinator.Domain.Aggregates;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Saga;
using MicroServiceSystem.Contracts.Events.Audit;
using MicroServiceSystem.Contracts.Events.Notification;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Coordinator.Infrastructure;

/// <summary>
/// Finishes or compensates RegisterUser sagas whose owner stopped making progress, for example because
/// the process died between a remote side effect and the next checkpoint. Ownership is decided by the
/// saga lease rather than by elapsed time, so a slow but live saga is never taken over.
/// </summary>
public sealed class RegisterUserSagaRecoveryService(
    IServiceScopeFactory scopeFactory,
    IOptions<SagaOptions> options,
    ILogger<RegisterUserSagaRecoveryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        SagaOptions settings = options.Value;

        if (!settings.RecoveryEnabled)
        {
            logger.LogInformation("RegisterUser saga recovery is disabled.");
            return;
        }

        int pollSeconds = Math.Max(5, settings.PollIntervalSeconds);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(pollSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RecoverBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "RegisterUser saga recovery poll failed.");
            }
        }
    }

    private async Task RecoverBatchAsync(CancellationToken cancellationToken)
    {
        SagaOptions settings = options.Value;
        var leaseDuration = TimeSpan.FromSeconds(Math.Max(30, settings.LeaseSeconds));

        IReadOnlyList<SagaCandidate> candidates = await ListCandidatesAsync(
            Math.Max(1, settings.BatchSize),
            cancellationToken);

        // One scope per saga on purpose: a losing claim throws a concurrency conflict, which leaves the
        // offending entity stuck in that context's change tracker and would poison every later save in
        // the batch. A disposable scope keeps the damage to the saga that caused it.
        foreach (SagaCandidate candidate in candidates)
        {
            try
            {
                await RecoverCandidateAsync(candidate, leaseDuration, cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                logger.LogDebug(
                    "Saga {SagaId} was advanced by another owner; leaving it for the next poll.",
                    candidate.Id);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Recovery failed for saga {SagaId}.", candidate.Id);
            }
        }
    }

    private async Task<IReadOnlyList<SagaCandidate>> ListCandidatesAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        var sagas = scope.ServiceProvider.GetRequiredService<IRegisterUserSagaRepository>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        // No ambient tenant here, which opens the tenant query filter so every tenant's stragglers are seen.
        IReadOnlyList<RegisterUserSaga> abandoned = await sagas.ListAbandonedAsync(
            clock.UtcNow,
            batchSize,
            cancellationToken);

        return [.. abandoned.Select(saga => new SagaCandidate(saga.Id, saga.TenantId))];
    }

    private async Task RecoverCandidateAsync(
        SagaCandidate candidate,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IServiceProvider provider = scope.ServiceProvider;

        var currentTenant = provider.GetRequiredService<ICurrentTenant>();
        using IDisposable tenantScope = currentTenant.Change(candidate.TenantId);

        var sagas = provider.GetRequiredService<IRegisterUserSagaRepository>();
        var clock = provider.GetRequiredService<IDateTimeProvider>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();

        if (await sagas.GetByIdAsync(candidate.Id, cancellationToken) is not { } saga)
        {
            return;
        }

        // Re-check under this context: the owner may have finished or renewed its lease since the scan.
        if (saga.IsTerminal || !saga.IsLeaseExpired(clock.UtcNow))
        {
            return;
        }

        // Claiming is the concurrency gate. If the token check fails, another worker got there first and
        // the exception unwinds to the caller, which discards this scope.
        saga.AcquireLease(SagaOwner.Current, clock.UtcNow, leaseDuration);
        sagas.Update(saga);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Recovering abandoned RegisterUser saga {SagaId} in state {State}",
            saga.Id,
            saga.State);

        switch (saga.State)
        {
            case RegisterUserSagaState.Started:
            case RegisterUserSagaState.IdentityRegistered:
            case RegisterUserSagaState.Compensating:
                // Started is not safe to simply fail: the saga reserves the identity id before calling
                // Identity, so the user may exist even though the response never came back. Disabling a
                // user that was never created is a no-op.
                await CompensateIdentityAsync(saga, provider, cancellationToken);
                break;

            case RegisterUserSagaState.UserProfileCreated:
                await CompleteAsync(saga, provider, cancellationToken);
                break;

            default:
                break;
        }
    }

    private static async Task CompensateIdentityAsync(
        RegisterUserSaga saga,
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        var sagas = provider.GetRequiredService<IRegisterUserSagaRepository>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var identityClient = provider.GetRequiredService<IIdentityServiceClient>();

        if (saga.IdentityUserId is not Guid identityUserId)
        {
            saga.MarkFailed("Recovery: abandoned before an identity was reserved.");
            sagas.Update(saga);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            await identityClient.DisableAsync(
                identityUserId,
                $"Recovery compensation for saga {saga.Id}",
                saga.TenantId,
                cancellationToken);

            saga.MarkFailed("Recovery: compensated abandoned identity registration.");
        }
        catch (Exception exception)
        {
            // Stay non-terminal so the next poll retries the undo once this lease lapses.
            saga.MarkCompensating($"Recovery: compensation failed: {exception.Message}");
        }

        sagas.Update(saga);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// The profile exists, so the workflow succeeded and only its follow-up events are missing. They are
    /// published here because the consumers deduplicate through the inbox.
    /// </summary>
    private static async Task CompleteAsync(
        RegisterUserSaga saga,
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        var sagas = provider.GetRequiredService<IRegisterUserSagaRepository>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var integrationEvents = provider.GetRequiredService<IIntegrationEventPublisher>();

        saga.MarkCompleted();
        sagas.Update(saga);

        if (saga.IdentityUserId is Guid identityUserId)
        {
            await integrationEvents.PublishAsync(
                new WelcomeNotificationRequestedIntegrationEvent
                {
                    UserId = identityUserId,
                    Email = saga.Email,
                    DisplayName = saga.DisplayName,
                    TenantId = saga.TenantId
                },
                cancellationToken);

            await integrationEvents.PublishAsync(
                new AuditEntryRequestedIntegrationEvent
                {
                    Action = "user.registered",
                    ResourceType = "User",
                    ResourceId = identityUserId.ToString(),
                    ActorUserId = identityUserId,
                    Details = $"User {saga.Email} registered via coordinator saga {saga.Id} (recovered)",
                    TenantId = saga.TenantId
                },
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private readonly record struct SagaCandidate(Guid Id, Guid TenantId);
}
