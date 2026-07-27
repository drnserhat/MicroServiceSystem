using MicroServiceSystem.SharedKernel.Guards;
using MicroServiceSystem.SharedKernel.Primitives;

namespace MicroServiceSystem.Services.Location.Domain.Aggregates;

public sealed class Country : TenantAggregateRoot<Guid>
{
    private Country()
    {
    }

    private Country(Guid id, string code, string name)
        : base(id)
    {
        Code = code;
        Name = name;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public static Country Create(string code, string name)
    {
        Ensure.NotNullOrWhiteSpace(code);
        Ensure.NotNullOrWhiteSpace(name);
        Ensure.MaxLength(code, 3);
        return new(Guid.CreateVersion7(), code.Trim().ToUpperInvariant(), name.Trim());
    }

    public void Rename(string name)
    {
        Ensure.NotNullOrWhiteSpace(name);
        Name = name.Trim();
    }
}
