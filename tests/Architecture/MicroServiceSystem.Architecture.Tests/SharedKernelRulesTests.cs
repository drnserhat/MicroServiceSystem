using System.Reflection;
using MicroServiceSystem.SharedKernel.Primitives;
using NetArchTest.Rules;
using Shouldly;

namespace MicroServiceSystem.Architecture.Tests;

public sealed class SharedKernelRulesTests
{
    private static readonly Assembly SharedKernelAssembly = typeof(Entity<>).Assembly;

    [Fact]
    public void SharedKernel_should_not_depend_on_infrastructure_libraries()
    {
        ArchTestResult result = Types.InAssembly(SharedKernelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ArchitectureNamespaces.PersistenceAndTransportLibraries)
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void SharedKernel_should_not_depend_on_aspnetcore()
    {
        ArchTestResult result = Types.InAssembly(SharedKernelAssembly)
            .ShouldNot()
            .HaveDependencyOn(ArchitectureNamespaces.AspNetCore)
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void SharedKernel_should_not_depend_on_mediatr()
    {
        ArchTestResult result = Types.InAssembly(SharedKernelAssembly)
            .ShouldNot()
            .HaveDependencyOn(ArchitectureNamespaces.MediatR)
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void Aggregate_roots_should_expose_domain_events_through_the_base_type()
    {
        ArchTestResult result = Types.InAssembly(SharedKernelAssembly)
            .That()
            .Inherit(typeof(AggregateRoot<>))
            .Should()
            .BeClasses()
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }
}
