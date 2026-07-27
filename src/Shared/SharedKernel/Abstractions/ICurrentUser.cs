namespace MicroServiceSystem.SharedKernel.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }

    string? UserName { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }

    IReadOnlyCollection<string> Roles { get; }

    IReadOnlyCollection<string> Permissions { get; }

    bool HasPermission(string permission);

    bool IsInRole(string role);
}
