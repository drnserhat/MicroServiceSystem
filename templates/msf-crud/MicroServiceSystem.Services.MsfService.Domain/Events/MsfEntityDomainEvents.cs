using MicroServiceSystem.SharedKernel.DomainEvents;

namespace MicroServiceSystem.Services.MsfService.Domain.Events;

public sealed record MsfEntityCreatedDomainEvent(Guid MsfEntityId, string Name) : DomainEvent;

public sealed record MsfEntityRenamedDomainEvent(Guid MsfEntityId, string Name) : DomainEvent;

public sealed record MsfEntityDeletedDomainEvent(Guid MsfEntityId) : DomainEvent;
