using System.Reflection;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using NetArchTest.Rules;
using Shouldly;

namespace MicroServiceSystem.Architecture.Tests;

public sealed class BuildingBlockRulesTests
{
    private static readonly Assembly ApplicationBuildingBlockAssembly = typeof(ICommand).Assembly;

    private static readonly Assembly MessagingBuildingBlockAssembly = typeof(IMessagePublisher).Assembly;

    [Fact]
    public void Application_building_block_should_not_depend_on_persistence_or_broker_libraries()
    {
        ArchTestResult result = Types.InAssembly(ApplicationBuildingBlockAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ArchitectureNamespaces.PersistenceAndTransportLibraries)
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void Messaging_building_block_should_not_depend_on_entity_framework()
    {
        ArchTestResult result = Types.InAssembly(MessagingBuildingBlockAssembly)
            .ShouldNot()
            .HaveDependencyOn(ArchitectureNamespaces.EntityFrameworkCore)
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void Building_blocks_should_not_depend_on_any_service_assembly()
    {
        Assembly[] buildingBlockAssemblies =
        [
            ApplicationBuildingBlockAssembly,
            MessagingBuildingBlockAssembly
        ];

        foreach (Assembly assembly in buildingBlockAssemblies)
        {
            assembly.GetReferencedAssemblies()
                .Select(referenced => referenced.Name ?? string.Empty)
                .Where(name => name.StartsWith("MicroServiceSystem.Services", StringComparison.Ordinal))
                .ShouldBeEmpty($"'{assembly.GetName().Name}' must stay independent from concrete services.");
        }
    }

    [Fact]
    public void Options_types_should_expose_a_configuration_section_name()
    {
        Assembly[] assemblies = [ApplicationBuildingBlockAssembly, MessagingBuildingBlockAssembly];

        Type[] optionTypes = [.. assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Name.EndsWith("Options", StringComparison.Ordinal) && type is { IsClass: true, IsAbstract: false })];

        foreach (Type optionType in optionTypes)
        {
            optionType.GetField("SectionName", BindingFlags.Public | BindingFlags.Static)
                .ShouldNotBeNull($"Options type '{optionType.FullName}' must declare a SectionName constant.");
        }
    }
}
