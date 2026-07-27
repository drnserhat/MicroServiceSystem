using MicroServiceSystem.SharedKernel.Guards;
using MicroServiceSystem.SharedKernel.Primitives;
namespace MicroServiceSystem.Services.Notification.Domain.Aggregates;
public sealed class NotificationMessage : TenantAggregateRoot<Guid>
{
    private NotificationMessage() { }
    private NotificationMessage(Guid id, Guid userId, string email, string displayName, string channel) : base(id) { UserId = userId; Email = email; DisplayName = displayName; Channel = channel; Status = "Pending"; }
    public Guid UserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Channel { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public static NotificationMessage Create(Guid userId, string email, string displayName, string channel = "Push")
    { Ensure.NotEmpty(userId); Ensure.NotNullOrWhiteSpace(email); Ensure.NotNullOrWhiteSpace(displayName); Ensure.NotNullOrWhiteSpace(channel); return new(Guid.CreateVersion7(), userId, email.Trim(), displayName.Trim(), channel); }
    public void MarkSent() => Status = "Sent";
}
