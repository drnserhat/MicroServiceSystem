using System.Reflection;
using MicroServiceSystem.Contracts.Abstractions;
using MicroServiceSystem.Contracts.Events.Identity;
using MicroServiceSystem.Contracts.Events.User;
using Shouldly;

namespace MicroServiceSystem.BuildingBlocks.IntegrationTests;

/// <summary>
/// Phase 3 contract gate: Register flow events are versioned and discoverable without spinning up the
/// full Compose stack.
/// </summary>
public sealed class Phase3RegisterContractTests
{
    [Fact]
    public void User_registered_event_has_stable_wire_name()
    {
        IntegrationEventNaming.Resolve(typeof(UserRegisteredIntegrationEvent))
            .ShouldBe("identity.user_registered.v1");
    }

    [Fact]
    public void User_profile_created_event_has_stable_wire_name()
    {
        IntegrationEventNaming.Resolve(typeof(UserProfileCreatedIntegrationEvent))
            .ShouldBe("user.profile_created.v1");
    }

    [Fact]
    public void Identity_and_user_apis_expose_register_surface()
    {
        Assembly identityApi = Assembly.Load("MicroServiceSystem.Services.Identity.Api");
        Assembly userApi = Assembly.Load("MicroServiceSystem.Services.User.Api");
        Assembly coordinatorApi = Assembly.Load("Coordinator.Api");

        identityApi.GetTypes().Any(type => type.Name == "AuthController").ShouldBeTrue();
        userApi.GetTypes().Any(type => type.Name.Contains("Controller", StringComparison.Ordinal)).ShouldBeTrue();
        coordinatorApi.GetTypes().Any(type => type.Name.Contains("Controller", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
