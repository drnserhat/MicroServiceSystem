namespace MicroServiceSystem.Services.Notification.Application.Abstractions;
public interface IPushSender{Task SendWelcomeAsync(Guid userId,string email,string displayName,CancellationToken cancellationToken=default);}
