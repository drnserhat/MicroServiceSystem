namespace MicroServiceSystem.SharedKernel.Abstractions;

public interface IAuditableEntity
{
    DateTimeOffset CreatedAtUtc { get; set; }

    string? CreatedBy { get; set; }

    DateTimeOffset? ModifiedAtUtc { get; set; }

    string? ModifiedBy { get; set; }
}
