using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.BuildingBlocks.MultiTenancy;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Identity.Application.Tenants;

public sealed record CreateTenantCommand(string Name, string Slug, Guid? TenantId = null)
    : ICommand<TenantResponse>;

public sealed record GetTenantQuery(Guid TenantId) : IQuery<TenantResponse>;

public sealed record TenantResponse(Guid Id, string Name, string Slug, bool IsActive);

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(TenantConstraints.NameMaxLength);
        RuleFor(command => command.Slug).NotEmpty().MaximumLength(TenantConstraints.SlugMaxLength);
        RuleFor(command => command.TenantId).NotEmpty().When(command => command.TenantId is not null);
    }
}

public sealed class GetTenantQueryValidator : AbstractValidator<GetTenantQuery>
{
    public GetTenantQueryValidator()
    {
        RuleFor(query => query.TenantId).NotEmpty();
    }
}

public sealed class CreateTenantCommandHandler(ITenantRepository tenants)
    : ICommandHandler<CreateTenantCommand, TenantResponse>
{
    public async Task<Result<TenantResponse>> Handle(
        CreateTenantCommand command,
        CancellationToken cancellationToken)
    {
        string slug = Tenant.NormalizeSlug(command.Slug);

        if (await tenants.FindBySlugAsync(slug, cancellationToken) is not null)
        {
            return IdentityErrors.TenantSlugTaken;
        }

        if (command.TenantId is { } requestedId
            && await tenants.GetByIdAsync(requestedId, cancellationToken) is not null)
        {
            return IdentityErrors.TenantAlreadyExists;
        }

        Tenant tenant = command.TenantId is { } id
            ? Tenant.Provision(id, command.Name, slug)
            : Tenant.Provision(command.Name, slug);

        await tenants.AddAsync(tenant, cancellationToken);

        return TenantMapping.ToResponse(tenant);
    }
}

public sealed class GetTenantQueryHandler(ITenantRepository tenants)
    : IQueryHandler<GetTenantQuery, TenantResponse>
{
    public async Task<Result<TenantResponse>> Handle(
        GetTenantQuery query,
        CancellationToken cancellationToken)
    {
        Tenant? tenant = await tenants.GetByIdAsync(query.TenantId, cancellationToken);

        return tenant is null
            ? IdentityErrors.TenantNotFound
            : TenantMapping.ToResponse(tenant);
    }
}

/// <summary>
/// Shared validation used by login/register/saga callers that accept a tenant id from the request body.
/// </summary>
public static class TenantAccess
{
    public static async Task<Result<TenantInfo>> RequireActiveAsync(
        ITenantStore tenants,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        TenantInfo? tenant = await tenants.FindAsync(tenantId, cancellationToken);

        if (tenant is null)
        {
            return IdentityErrors.TenantNotFound;
        }

        if (!tenant.IsActive)
        {
            return IdentityErrors.TenantInactive;
        }

        return tenant;
    }
}

file static class TenantMapping
{
    public static TenantResponse ToResponse(Tenant tenant) =>
        new(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive);
}
