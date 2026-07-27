using System.Text.Json.Serialization;
using MicroServiceSystem.Contracts.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Serialization;

/// <summary>
/// Pre-generates the metadata for the envelope, which is the one shape every published and consumed
/// message goes through. Service specific event types stay on the reflection resolver because they are
/// only known at runtime.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(IntegrationEventEnvelope))]
internal sealed partial class MessagingJsonContext : JsonSerializerContext;
