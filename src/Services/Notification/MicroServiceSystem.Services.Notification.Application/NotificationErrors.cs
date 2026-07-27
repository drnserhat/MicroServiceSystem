using MicroServiceSystem.SharedKernel.Results;
namespace MicroServiceSystem.Services.Notification.Application;
public static class NotificationErrors
{
    public static readonly Error NotFound = Error.NotFound("notification.not_found", "NotificationMessage was not found.");
}
