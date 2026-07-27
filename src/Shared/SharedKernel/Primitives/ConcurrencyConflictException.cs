using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.SharedKernel.Primitives;

/// <summary>
/// Raised when a write lost an optimistic concurrency check. It exists so the persistence provider's
/// own exception type does not have to leak into the API layer to be mapped to a 409.
/// </summary>
public sealed class ConcurrencyConflictException(string message, Exception innerException)
    : DomainException(FrameworkErrorCodes.Concurrency, message, innerException);
