using MicroServiceSystem.SharedKernel.Results;
namespace MicroServiceSystem.Services.Audit.Application;
public static class AuditErrors
{
    public static readonly Error NotFound = Error.NotFound("audit.not_found", "AuditEntry was not found.");
}
