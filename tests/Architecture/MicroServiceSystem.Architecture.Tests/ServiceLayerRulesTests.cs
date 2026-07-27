using System.Reflection;
using NetArchTest.Rules;
using Shouldly;

namespace MicroServiceSystem.Architecture.Tests;

/// <summary>
/// Service-layer boundaries for Identity (representative of every bounded context). Domain stays free
/// of Application/Infrastructure/Persistence; Application stays free of Persistence/EF.
/// </summary>
public sealed class ServiceLayerRulesTests
{
    private static readonly Assembly IdentityDomain =
        Assembly.Load("MicroServiceSystem.Services.Identity.Domain");

    private static readonly Assembly IdentityApplication =
        Assembly.Load("MicroServiceSystem.Services.Identity.Application");

    [Fact]
    public void Identity_domain_does_not_reference_application_or_infrastructure()
    {
        ArchTestResult result = Types.InAssembly(IdentityDomain)
            .ShouldNot()
            .HaveDependencyOnAny(
                "MicroServiceSystem.Services.Identity.Application",
                "MicroServiceSystem.Services.Identity.Infrastructure",
                "MicroServiceSystem.Services.Identity.Persistence",
                "MicroServiceSystem.Services.Identity.Api",
                ArchitectureNamespaces.EntityFrameworkCore,
                ArchitectureNamespaces.AspNetCore,
                ArchitectureNamespaces.MediatR)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Format(result));
    }

    [Fact]
    public void Identity_application_does_not_reference_persistence_or_ef()
    {
        ArchTestResult result = Types.InAssembly(IdentityApplication)
            .ShouldNot()
            .HaveDependencyOnAny(
                "MicroServiceSystem.Services.Identity.Persistence",
                "MicroServiceSystem.Services.Identity.Infrastructure",
                "MicroServiceSystem.Services.Identity.Api",
                ArchitectureNamespaces.EntityFrameworkCore,
                ArchitectureNamespaces.AspNetCore)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Format(result));
    }

    private static string Format(ArchTestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : string.Join(Environment.NewLine, result.FailingTypeNames ?? []);
}
