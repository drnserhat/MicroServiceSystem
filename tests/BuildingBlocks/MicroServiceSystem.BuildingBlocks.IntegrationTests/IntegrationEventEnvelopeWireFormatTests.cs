using System.Text;
using System.Text.Json;
using MicroServiceSystem.BuildingBlocks.Messaging.Serialization;
using MicroServiceSystem.Contracts.Abstractions;
using Shouldly;

namespace MicroServiceSystem.BuildingBlocks.IntegrationTests;

public sealed class IntegrationEventEnvelopeWireFormatTests
{
    [Fact]
    public void Payload_is_carried_inline_instead_of_being_escaped()
    {
        IntegrationEventEnvelope envelope = CreateEnvelope("""{"name":"value","count":3}""");

        string json = JsonSerializer.Serialize(envelope, IntegrationEventSerializer.SerializerOptions);

        json.ShouldContain("""
            "payload":{"name":"value","count":3}
            """);
        json.ShouldNotContain("\\\"");
    }

    [Fact]
    public void Inline_payload_survives_a_round_trip()
    {
        IntegrationEventEnvelope envelope = CreateEnvelope("""{"name":"value","nested":{"flag":true}}""");

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(envelope, IntegrationEventSerializer.SerializerOptions);

        IntegrationEventEnvelope? restored = JsonSerializer.Deserialize<IntegrationEventEnvelope>(
            body.AsSpan(),
            IntegrationEventSerializer.SerializerOptions);

        restored.ShouldNotBeNull();
        restored.MessageId.ShouldBe(envelope.MessageId);
        restored.EventName.ShouldBe(envelope.EventName);
        restored.TenantId.ShouldBe(envelope.TenantId);
        restored.AttemptCount.ShouldBe(envelope.AttemptCount);
        JsonSerializer.Deserialize<JsonElement>(restored.Payload)
            .GetProperty("nested")
            .GetProperty("flag")
            .GetBoolean()
            .ShouldBeTrue();
    }

    [Fact]
    public void Messages_published_before_the_inline_payload_change_still_deserialize()
    {
        // Shape produced by the previous publisher: the document nested as an escaped JSON string.
        const string Legacy = """
            {"messageId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","eventName":"tests.legacy.v1","payload":"{\"name\":\"value\"}","occurredOnUtc":"2026-01-01T00:00:00+00:00","attemptCount":1}
            """;

        IntegrationEventEnvelope? restored = JsonSerializer.Deserialize<IntegrationEventEnvelope>(
            Encoding.UTF8.GetBytes(Legacy).AsSpan(),
            IntegrationEventSerializer.SerializerOptions);

        restored.ShouldNotBeNull();
        restored.EventName.ShouldBe("tests.legacy.v1");
        restored.Payload.ShouldBe("""{"name":"value"}""");
    }

    private static IntegrationEventEnvelope CreateEnvelope(string payload) =>
        new()
        {
            MessageId = Guid.NewGuid(),
            EventName = "tests.wire.v1",
            Payload = payload,
            OccurredOnUtc = DateTimeOffset.UtcNow,
            TenantId = Guid.NewGuid(),
            CorrelationId = "correlation-1",
            Source = "tests",
            AttemptCount = 1
        };
}
