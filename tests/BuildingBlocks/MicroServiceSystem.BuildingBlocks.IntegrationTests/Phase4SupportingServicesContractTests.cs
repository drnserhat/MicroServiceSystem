using System.Reflection;
using MicroServiceSystem.Contracts.Abstractions;
using MicroServiceSystem.Contracts.Events.Audit;
using MicroServiceSystem.Contracts.Events.Notification;
using Shouldly;

namespace MicroServiceSystem.BuildingBlocks.IntegrationTests;

/// <summary>
/// Phase 4 gate: supporting services expose API hosts and consume the shared event contracts.
/// </summary>
public sealed class Phase4SupportingServicesContractTests
{
    [Theory]
    [InlineData("MicroServiceSystem.Services.Notification.Api")]
    [InlineData("MicroServiceSystem.Services.File.Api")]
    [InlineData("MicroServiceSystem.Services.Audit.Api")]
    [InlineData("MicroServiceSystem.Services.Settings.Api")]
    [InlineData("MicroServiceSystem.Services.Location.Api")]
    [InlineData("MicroServiceSystem.Services.Logging.Api")]
    public void Supporting_service_api_assembly_loads(string assemblyName)
    {
        Assembly.Load(assemblyName).GetName().Name.ShouldBe(assemblyName);
    }

    [Fact]
    public void Supporting_integration_events_keep_stable_names()
    {
        IntegrationEventNaming.Resolve(typeof(WelcomeNotificationRequestedIntegrationEvent))
            .ShouldBe("notification.welcome_requested.v1");

        IntegrationEventNaming.Resolve(typeof(AuditEntryRequestedIntegrationEvent))
            .ShouldBe("audit.entry_requested.v1");
    }
}
