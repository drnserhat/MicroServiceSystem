using MicroServiceSystem.BuildingBlocks.Authentication.Abstractions;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Application.Auth;
using MicroServiceSystem.Services.Identity.Domain;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MicroServiceSystem.Services.Identity.Infrastructure;

/// <summary>
/// Seeds a tenant admin for the README demo tenant so local registration samples can authenticate
/// after anonymous self-signup was closed. Production must provision admins explicitly.
/// </summary>
public sealed class DevelopmentAdminSeeder(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    ILogger<DevelopmentAdminSeeder> logger) : IHostedService
{
    public static readonly Guid AdminUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public const string AdminEmail = "admin@dev.local";

    public const string AdminUserName = "devadmin";

    public const string AdminPassword = "DevAdmin!Pass1";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IIdentityUserRepository users = scope.ServiceProvider.GetRequiredService<IIdentityUserRepository>();
            IRoleRepository roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
            IPasswordHasher passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            ICurrentTenant currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();

            using IDisposable tenantScope = currentTenant.Change(KnownTenants.DevelopmentDemo, "Development Demo");

            Role adminRole = await AccessTokenFactory.GetOrCreateAdminRoleAsync(
                roles,
                KnownTenants.DevelopmentDemo,
                cancellationToken);

            if (await users.FindByEmailAsync(AdminEmail, cancellationToken) is not null)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            IdentityUser admin = IdentityUser.Register(
                AdminUserId,
                AdminEmail,
                AdminUserName,
                passwordHasher.Hash(AdminPassword));

            admin.TenantId = KnownTenants.DevelopmentDemo;
            admin.AssignRole(adminRole.Id);

            await users.AddAsync(admin, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Seeded development admin {Email} for tenant {TenantId}",
                AdminEmail,
                KnownTenants.DevelopmentDemo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed development admin {Email}", AdminEmail);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
