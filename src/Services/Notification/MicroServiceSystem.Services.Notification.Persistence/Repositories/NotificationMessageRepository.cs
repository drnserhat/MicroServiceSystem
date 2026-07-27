using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.Repositories;
using MicroServiceSystem.Services.Notification.Application.Abstractions;
using MicroServiceSystem.Services.Notification.Domain.Aggregates;
namespace MicroServiceSystem.Services.Notification.Persistence.Repositories;
public sealed class NotificationMessageRepository(NotificationDbContext context) : EfRepository<NotificationMessage, Guid>(context), INotificationMessageRepository
{
    public async Task<IReadOnlyList<NotificationMessage>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().ToListAsync(cancellationToken);
}
