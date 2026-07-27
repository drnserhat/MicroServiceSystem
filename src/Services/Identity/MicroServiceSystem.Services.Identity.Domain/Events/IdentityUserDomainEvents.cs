using MicroServiceSystem.SharedKernel.DomainEvents;

namespace MicroServiceSystem.Services.Identity.Domain.Events;

public sealed record IdentityUserRegisteredDomainEvent(Guid UserId, string Email, string UserName) : DomainEvent;

public sealed record IdentityUserDisabledDomainEvent(Guid UserId, string Reason) : DomainEvent;
