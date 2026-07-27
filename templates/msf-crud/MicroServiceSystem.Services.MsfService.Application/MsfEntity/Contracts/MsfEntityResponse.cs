namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Contracts;

public sealed record MsfEntityResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModifiedAtUtc);
