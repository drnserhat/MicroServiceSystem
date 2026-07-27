using MicroServiceSystem.Services.Notification.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
namespace MicroServiceSystem.Services.Notification.Application.Abstractions;
public interface INotificationMessageRepository : IRepository<NotificationMessage, Guid>
{
    Task<IReadOnlyList<NotificationMessage>> ListAllAsync(CancellationToken cancellationToken = default);
}
