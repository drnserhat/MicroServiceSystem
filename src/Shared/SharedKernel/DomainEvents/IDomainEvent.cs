namespace MicroServiceSystem.SharedKernel.DomainEvents;

public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredOnUtc { get; }
}
