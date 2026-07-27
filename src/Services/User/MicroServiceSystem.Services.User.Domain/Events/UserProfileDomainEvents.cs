using MicroServiceSystem.SharedKernel.DomainEvents;

namespace MicroServiceSystem.Services.User.Domain.Events;

public sealed record UserProfileCreatedDomainEvent(Guid UserId, string DisplayName) : DomainEvent;

public sealed record UserProfileUpdatedDomainEvent(Guid UserId, string DisplayName) : DomainEvent;

public sealed record UserProfileDeactivatedDomainEvent(Guid UserId, string Reason) : DomainEvent;
