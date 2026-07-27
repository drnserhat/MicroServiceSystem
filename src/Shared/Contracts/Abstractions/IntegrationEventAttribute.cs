namespace MicroServiceSystem.Contracts.Abstractions;

/// <summary>
/// Declares the wire name of an integration event. The name is the single source of truth for
/// routing keys and queue bindings, which keeps broker topology free of magic strings.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IntegrationEventAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
