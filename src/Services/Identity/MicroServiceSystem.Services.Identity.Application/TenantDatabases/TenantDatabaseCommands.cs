using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Contracts.Events.Identity;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Identity.Application.TenantDatabases;

public sealed record ListTenantDatabasesQuery(Guid TenantId) : IQuery<IReadOnlyList<TenantDatabaseBindingResponse>>;

public sealed record ProvisionTenantDatabaseCommand(Guid TenantId, string ServiceKey)
    : ICommand<TenantDatabaseBindingResponse>;

public sealed record RetryTenantDatabaseCommand(Guid TenantId, string ServiceKey)
    : ICommand<TenantDatabaseBindingResponse>;

public sealed record HealthTenantDatabaseCommand(Guid TenantId, string ServiceKey)
    : ICommand<TenantDatabaseHealthResponse>;

public sealed record ResolveTenantDatabaseBindingQuery(Guid TenantId, string ServiceKey)
    : IQuery<TenantDatabaseResolveResponse>;

public sealed record TenantDatabaseBindingResponse(
    Guid Id,
    Guid TenantId,
    string ServiceKey,
    Guid ClusterId,
    string ClusterSlug,
    string DatabaseName,
    string Username,
    string Status,
    string? SchemaVersion,
    string? LastError);

public sealed record TenantDatabaseHealthResponse(bool Healthy, string Status, string? Detail);

public sealed record TenantDatabaseResolveResponse(
    Guid TenantId,
    string ServiceKey,
    string Host,
    int Port,
    string DatabaseName,
    string Username,
    string SecretRef,
    string Status,
    string? SchemaVersion);

public sealed class ListTenantDatabasesQueryValidator : AbstractValidator<ListTenantDatabasesQuery>
{
    public ListTenantDatabasesQueryValidator() => RuleFor(query => query.TenantId).NotEmpty();
}

public sealed class ProvisionTenantDatabaseCommandValidator : AbstractValidator<ProvisionTenantDatabaseCommand>
{
    public ProvisionTenantDatabaseCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.ServiceKey).NotEmpty().MaximumLength(TenantDatabaseBindingConstraints.ServiceKeyMaxLength);
    }
}

public sealed class RetryTenantDatabaseCommandValidator : AbstractValidator<RetryTenantDatabaseCommand>
{
    public RetryTenantDatabaseCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.ServiceKey).NotEmpty().MaximumLength(TenantDatabaseBindingConstraints.ServiceKeyMaxLength);
    }
}

public sealed class HealthTenantDatabaseCommandValidator : AbstractValidator<HealthTenantDatabaseCommand>
{
    public HealthTenantDatabaseCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.ServiceKey).NotEmpty().MaximumLength(TenantDatabaseBindingConstraints.ServiceKeyMaxLength);
    }
}

public sealed class ResolveTenantDatabaseBindingQueryValidator : AbstractValidator<ResolveTenantDatabaseBindingQuery>
{
    public ResolveTenantDatabaseBindingQueryValidator()
    {
        RuleFor(query => query.TenantId).NotEmpty();
        RuleFor(query => query.ServiceKey).NotEmpty().MaximumLength(TenantDatabaseBindingConstraints.ServiceKeyMaxLength);
    }
}

public sealed class ListTenantDatabasesQueryHandler(
    ITenantRepository tenants,
    ITenantDatabaseBindingRepository bindings,
    IPostgresClusterRepository clusters)
    : IQueryHandler<ListTenantDatabasesQuery, IReadOnlyList<TenantDatabaseBindingResponse>>
{
    public async Task<Result<IReadOnlyList<TenantDatabaseBindingResponse>>> Handle(
        ListTenantDatabasesQuery query,
        CancellationToken cancellationToken)
    {
        if (await tenants.GetByIdAsync(query.TenantId, cancellationToken) is null)
        {
            return IdentityErrors.TenantNotFound;
        }

        IReadOnlyList<TenantDatabaseBinding> list =
            await bindings.ListByTenantAsync(query.TenantId, cancellationToken);

        List<TenantDatabaseBindingResponse> responses = [];
        foreach (TenantDatabaseBinding binding in list)
        {
            PostgresCluster? cluster = await clusters.GetByIdAsync(binding.ClusterId, cancellationToken);
            responses.Add(TenantDatabaseMapping.ToResponse(binding, cluster?.Slug ?? string.Empty));
        }

        return responses;
    }
}

public sealed class ProvisionTenantDatabaseCommandHandler(
    ITenantRepository tenants,
    ITenantDatabaseBindingRepository bindings,
    IPostgresClusterRepository clusters,
    ITenantDatabaseProvisioner provisioner,
    IIntegrationEventPublisher integrationEvents)
    : ICommandHandler<ProvisionTenantDatabaseCommand, TenantDatabaseBindingResponse>
{
    public async Task<Result<TenantDatabaseBindingResponse>> Handle(
        ProvisionTenantDatabaseCommand command,
        CancellationToken cancellationToken)
    {
        if (!KnownServiceKeys.IsAllowed(command.ServiceKey))
        {
            return IdentityErrors.ServiceKeyNotAllowed;
        }

        Tenant? tenant = await tenants.GetByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
        {
            return IdentityErrors.TenantNotFound;
        }

        if (!tenant.IsActive)
        {
            return IdentityErrors.TenantInactive;
        }

        string serviceKey = KnownServiceKeys.Normalize(command.ServiceKey);
        TenantDatabaseBinding? existing =
            await bindings.FindByTenantAndServiceAsync(command.TenantId, serviceKey, cancellationToken);

        if (existing is { Status: TenantDatabaseStatus.Ready or TenantDatabaseStatus.Migrating or TenantDatabaseStatus.Provisioning })
        {
            PostgresCluster? existingCluster = await clusters.GetByIdAsync(existing.ClusterId, cancellationToken);
            return TenantDatabaseMapping.ToResponse(existing, existingCluster?.Slug ?? string.Empty);
        }

        PostgresCluster? cluster = await clusters.FindDefaultAsync(cancellationToken);
        if (cluster is null || !cluster.IsActive)
        {
            return IdentityErrors.PostgresClusterNotFound;
        }

        TenantDatabaseBinding binding;
        if (existing is null)
        {
            binding = TenantDatabaseBinding.StartProvision(
                command.TenantId,
                serviceKey,
                cluster.Id,
                BuildDatabaseName(serviceKey, tenant.Slug),
                BranchDatabaseDefaults.AppUsername,
                BranchDatabaseDefaults.AppPasswordSecretRef);
            await bindings.AddAsync(binding, cancellationToken);
        }
        else
        {
            existing.RestartProvision();
            binding = existing;
        }

        Result provisionResult = await provisioner.ProvisionAsync(binding, cluster, cancellationToken);
        if (provisionResult.IsFailure)
        {
            binding.MarkFailed(provisionResult.Error.Description);
            await PublishAccessChangedAsync(binding, cancellationToken);
            return TenantDatabaseMapping.ToResponse(binding, cluster.Slug);
        }

        await PublishAccessChangedAsync(binding, cancellationToken);
        return TenantDatabaseMapping.ToResponse(binding, cluster.Slug);
    }

    private Task PublishAccessChangedAsync(TenantDatabaseBinding binding, CancellationToken cancellationToken) =>
        integrationEvents.PublishAsync(
            new TenantDatabaseAccessChangedIntegrationEvent
            {
                BindingTenantId = binding.TenantId,
                ServiceKey = binding.ServiceKey,
                Status = binding.Status.ToString(),
                TenantId = binding.TenantId
            },
            cancellationToken);

    internal static string BuildDatabaseName(string serviceKey, string tenantSlug)
    {
        string raw = $"{serviceKey}_{tenantSlug}".ToLowerInvariant();
        string sanitized = new(raw.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_').ToArray());
        if (sanitized.Length > TenantDatabaseBindingConstraints.DatabaseNameMaxLength)
        {
            sanitized = sanitized[..TenantDatabaseBindingConstraints.DatabaseNameMaxLength];
        }

        return sanitized;
    }
}

public sealed class RetryTenantDatabaseCommandHandler(
    ITenantRepository tenants,
    ITenantDatabaseBindingRepository bindings,
    IPostgresClusterRepository clusters,
    ITenantDatabaseProvisioner provisioner,
    IIntegrationEventPublisher integrationEvents)
    : ICommandHandler<RetryTenantDatabaseCommand, TenantDatabaseBindingResponse>
{
    public async Task<Result<TenantDatabaseBindingResponse>> Handle(
        RetryTenantDatabaseCommand command,
        CancellationToken cancellationToken)
    {
        if (!KnownServiceKeys.IsAllowed(command.ServiceKey))
        {
            return IdentityErrors.ServiceKeyNotAllowed;
        }

        if (await tenants.GetByIdAsync(command.TenantId, cancellationToken) is not { IsActive: true })
        {
            Tenant? tenant = await tenants.GetByIdAsync(command.TenantId, cancellationToken);
            return tenant is null ? IdentityErrors.TenantNotFound : IdentityErrors.TenantInactive;
        }

        TenantDatabaseBinding? binding = await bindings.FindByTenantAndServiceAsync(
            command.TenantId,
            command.ServiceKey,
            cancellationToken);

        if (binding is null)
        {
            return IdentityErrors.TenantDatabaseBindingNotFound;
        }

        PostgresCluster? cluster = await clusters.GetByIdAsync(binding.ClusterId, cancellationToken);
        if (cluster is null || !cluster.IsActive)
        {
            return IdentityErrors.PostgresClusterNotFound;
        }

        binding.RestartProvision();

        Result provisionResult = await provisioner.ProvisionAsync(binding, cluster, cancellationToken);
        if (provisionResult.IsFailure)
        {
            binding.MarkFailed(provisionResult.Error.Description);
            await integrationEvents.PublishAsync(
                new TenantDatabaseAccessChangedIntegrationEvent
                {
                    BindingTenantId = binding.TenantId,
                    ServiceKey = binding.ServiceKey,
                    Status = binding.Status.ToString(),
                    TenantId = binding.TenantId
                },
                cancellationToken);
            return TenantDatabaseMapping.ToResponse(binding, cluster.Slug);
        }

        await integrationEvents.PublishAsync(
            new TenantDatabaseAccessChangedIntegrationEvent
            {
                BindingTenantId = binding.TenantId,
                ServiceKey = binding.ServiceKey,
                Status = binding.Status.ToString(),
                TenantId = binding.TenantId
            },
            cancellationToken);

        return TenantDatabaseMapping.ToResponse(binding, cluster.Slug);
    }
}

public sealed class HealthTenantDatabaseCommandHandler(
    ITenantDatabaseBindingRepository bindings,
    IPostgresClusterRepository clusters,
    ITenantDatabaseProvisioner provisioner)
    : ICommandHandler<HealthTenantDatabaseCommand, TenantDatabaseHealthResponse>
{
    public async Task<Result<TenantDatabaseHealthResponse>> Handle(
        HealthTenantDatabaseCommand command,
        CancellationToken cancellationToken)
    {
        if (!KnownServiceKeys.IsAllowed(command.ServiceKey))
        {
            return IdentityErrors.ServiceKeyNotAllowed;
        }

        TenantDatabaseBinding? binding = await bindings.FindByTenantAndServiceAsync(
            command.TenantId,
            command.ServiceKey,
            cancellationToken);

        if (binding is null)
        {
            return IdentityErrors.TenantDatabaseBindingNotFound;
        }

        PostgresCluster? cluster = await clusters.GetByIdAsync(binding.ClusterId, cancellationToken);
        if (cluster is null)
        {
            return IdentityErrors.PostgresClusterNotFound;
        }

        Result health = await provisioner.PingAsync(binding, cluster, cancellationToken);
        if (health.IsFailure)
        {
            binding.MarkDegraded(health.Error.Description);
            return new TenantDatabaseHealthResponse(false, binding.Status.ToString(), health.Error.Description);
        }

        if (binding.Status == TenantDatabaseStatus.Degraded)
        {
            binding.MarkReady(binding.SchemaVersion);
        }

        return new TenantDatabaseHealthResponse(true, binding.Status.ToString(), null);
    }
}

public sealed class ResolveTenantDatabaseBindingQueryHandler(
    ITenantDatabaseBindingRepository bindings,
    IPostgresClusterRepository clusters)
    : IQueryHandler<ResolveTenantDatabaseBindingQuery, TenantDatabaseResolveResponse>
{
    public async Task<Result<TenantDatabaseResolveResponse>> Handle(
        ResolveTenantDatabaseBindingQuery query,
        CancellationToken cancellationToken)
    {
        if (!KnownServiceKeys.IsAllowed(query.ServiceKey))
        {
            return IdentityErrors.ServiceKeyNotAllowed;
        }

        TenantDatabaseBinding? binding = await bindings.FindByTenantAndServiceAsync(
            query.TenantId,
            query.ServiceKey,
            cancellationToken);

        if (binding is null)
        {
            return IdentityErrors.TenantDatabaseBindingNotFound;
        }

        if (binding.Status != TenantDatabaseStatus.Ready)
        {
            return IdentityErrors.TenantDatabaseNotReady;
        }

        PostgresCluster? cluster = await clusters.GetByIdAsync(binding.ClusterId, cancellationToken);
        if (cluster is null || !cluster.IsActive)
        {
            return IdentityErrors.PostgresClusterNotFound;
        }

        return new TenantDatabaseResolveResponse(
            binding.TenantId,
            binding.ServiceKey,
            cluster.Host,
            cluster.Port,
            binding.DatabaseName,
            binding.Username,
            binding.SecretRef,
            binding.Status.ToString(),
            binding.SchemaVersion);
    }
}

public static class BranchDatabaseDefaults
{
    public const string AppUsername = "msf";

    /// <summary>Config key holding the shared app-role password for Compose Phase 1.</summary>
    public const string AppPasswordSecretRef = "Persistence:Postgres:AppPassword";

    /// <summary>Config key holding admin connection string used for CREATE DATABASE / SELECT 1.</summary>
    public const string AdminConnectionSecretRef = "Persistence:Postgres:AdminConnection";

    public const string DefaultClusterSlug = "local";
}

file static class TenantDatabaseMapping
{
    public static TenantDatabaseBindingResponse ToResponse(TenantDatabaseBinding binding, string clusterSlug) =>
        new(
            binding.Id,
            binding.TenantId,
            binding.ServiceKey,
            binding.ClusterId,
            clusterSlug,
            binding.DatabaseName,
            binding.Username,
            binding.Status.ToString(),
            binding.SchemaVersion,
            binding.LastError);
}
