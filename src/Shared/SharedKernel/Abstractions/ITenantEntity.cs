namespace MicroServiceSystem.SharedKernel.Abstractions;

public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
