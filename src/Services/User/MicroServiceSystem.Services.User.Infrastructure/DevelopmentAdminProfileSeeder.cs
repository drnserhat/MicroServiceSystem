using MicroServiceSystem.Services.User.Application.Abstractions;
using MicroServiceSystem.Services.User.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MicroServiceSystem.Services.User.Infrastructure;

/// <summary>
/// Seeds a User profile for the Identity development admin so Admin UI profile pages work locally.
/// Ids must stay aligned with <c>DevelopmentAdminSeeder</c> / <c>KnownTenants.DevelopmentDemo</c>.
/// </summary>
public sealed class DevelopmentAdminProfileSeeder(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    ILogger<DevelopmentAdminProfileSeeder> logger) : IHostedService
{
    public static readonly Guid DemoTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid AdminUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            ICurrentTenant currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
            using IDisposable tenantScope = currentTenant.Change(DemoTenantId, "Development Demo");

            IUserProfileRepository profiles = scope.ServiceProvider.GetRequiredService<IUserProfileRepository>();
            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            if (await profiles.GetByIdAsync(AdminUserId, cancellationToken) is not null)
            {
                return;
            }

            UserProfile profile = UserProfile.Create(AdminUserId, "Dev", "Admin", "Dev Admin");
            profile.TenantId = DemoTenantId;
            await profiles.AddAsync(profile, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Seeded development admin profile {UserId} for tenant {TenantId}",
                AdminUserId,
                DemoTenantId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed development admin profile {UserId}", AdminUserId);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
