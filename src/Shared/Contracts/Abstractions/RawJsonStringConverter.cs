using System.Text.Json;
using System.Text.Json.Serialization;

namespace MicroServiceSystem.Contracts.Abstractions;

/// <summary>
/// Carries an already serialized JSON document inline instead of nesting it as an escaped string.
/// Escaping the payload would make the broker frame noticeably larger and force a second parse on the
/// consumer just to unwrap it.
/// </summary>
public sealed class RawJsonStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return string.Empty;

            // Producers that predate inline payloads embedded the document as an escaped string.
            case JsonTokenType.String:
                return reader.GetString() ?? string.Empty;

            default:
                using (JsonDocument document = JsonDocument.ParseValue(ref reader))
                {
                    return document.RootElement.GetRawText();
                }
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (string.IsNullOrWhiteSpace(value))
        {
            writer.WriteNullValue();
            return;
        }

        // The payload is produced by this framework's own serializer, so re-validating it on every
        // publish would only repeat work that already happened.
        writer.WriteRawValue(value, skipInputValidation: true);
    }
}
