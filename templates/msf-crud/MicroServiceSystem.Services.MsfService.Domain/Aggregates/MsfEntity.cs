using MicroServiceSystem.Services.MsfService.Domain.Events;
using MicroServiceSystem.SharedKernel.Guards;
using MicroServiceSystem.SharedKernel.Primitives;

namespace MicroServiceSystem.Services.MsfService.Domain.Aggregates;

public sealed class MsfEntity : TenantAggregateRoot<Guid>
{
    private MsfEntity()
    {
    }

    private MsfEntity(Guid id, string name, string? description)
        : base(id)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public static MsfEntity Create(string name, string? description)
    {
        Ensure.NotNullOrWhiteSpace(name);
        Ensure.MaxLength(name, MsfEntityConstraints.NameMaxLength);

        var entity = new MsfEntity(Guid.CreateVersion7(), name, description);
        entity.RaiseDomainEvent(new MsfEntityCreatedDomainEvent(entity.Id, entity.Name));

        return entity;
    }

    public void Rename(string name)
    {
        Ensure.NotNullOrWhiteSpace(name);
        Ensure.MaxLength(name, MsfEntityConstraints.NameMaxLength);

        if (string.Equals(Name, name, StringComparison.Ordinal))
        {
            return;
        }

        Name = name;
        RaiseDomainEvent(new MsfEntityRenamedDomainEvent(Id, name));
    }

    public void ChangeDescription(string? description)
    {
        if (description is not null)
        {
            Ensure.MaxLength(description, MsfEntityConstraints.DescriptionMaxLength);
        }

        Description = description;
    }
}
