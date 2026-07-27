using System.Reflection;
using MicroServiceSystem.Contracts.Abstractions;
using NetArchTest.Rules;
using Shouldly;

namespace MicroServiceSystem.Architecture.Tests;

public sealed class ContractsRulesTests
{
    private static readonly Assembly ContractsAssembly = typeof(IntegrationEvent).Assembly;

    [Fact]
    public void Contracts_should_not_depend_on_infrastructure_libraries()
    {
        ArchTestResult result = Types.InAssembly(ContractsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ArchitectureNamespaces.PersistenceAndTransportLibraries)
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void Contracts_should_not_depend_on_the_shared_kernel()
    {
        AssemblyName[] referencedAssemblies = ContractsAssembly.GetReferencedAssemblies();

        referencedAssemblies
            .Select(assembly => assembly.Name)
            .ShouldNotContain("MicroServiceSystem.SharedKernel");
    }

    [Fact]
    public void Integration_events_should_declare_a_wire_name()
    {
        Type[] eventTypes = [.. ContractsAssembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsClass: true } && type.IsAssignableTo(typeof(IIntegrationEvent)))];

        foreach (Type eventType in eventTypes)
        {
            eventType.GetCustomAttribute<IntegrationEventAttribute>()
                .ShouldNotBeNull($"Integration event '{eventType.FullName}' must declare a wire name.");
        }
    }
}
