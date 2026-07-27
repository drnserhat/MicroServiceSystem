using MicroServiceSystem.SharedKernel.Guards;
using MicroServiceSystem.SharedKernel.Primitives;
namespace MicroServiceSystem.Services.Audit.Domain.Aggregates;
public sealed class AuditEntry : TenantAggregateRoot<Guid>
{
    private AuditEntry() { }
    private AuditEntry(Guid id,string action,string type,string resourceId,Guid? actor,string? details):base(id){Action=action;ResourceType=type;ResourceId=resourceId;ActorUserId=actor;Details=details;}
    public string Action { get; private set; }=string.Empty; public string ResourceType { get; private set; }=string.Empty; public string ResourceId { get; private set; }=string.Empty; public Guid? ActorUserId { get; private set; } public string? Details { get; private set; }
    public static AuditEntry Create(string action,string resourceType,string resourceId,Guid? actorUserId,string? details){Ensure.NotNullOrWhiteSpace(action);Ensure.NotNullOrWhiteSpace(resourceType);Ensure.NotNullOrWhiteSpace(resourceId);return new(Guid.CreateVersion7(),action,resourceType,resourceId,actorUserId,details);}
}
