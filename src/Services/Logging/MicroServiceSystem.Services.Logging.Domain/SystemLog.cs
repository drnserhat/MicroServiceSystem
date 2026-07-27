using MicroServiceSystem.SharedKernel.Primitives;
namespace MicroServiceSystem.Services.Logging.Domain;
public sealed class SystemLog:TenantAggregateRoot<Guid>{private SystemLog():base(Guid.Empty){} public string Message{get;private set;}=string.Empty;}
