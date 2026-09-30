using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace N2N_Saenai.Serialization;

/// <summary>Accepts API identifiers encoded either as a JSON string or JSON number.</summary>
public sealed class StringOrNumberConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String) return reader.GetString();
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType == JsonTokenType.Number)
        {
            using var number = JsonDocument.ParseValue(ref reader);
            return number.RootElement.GetRawText();
        }
        throw new JsonException("Expected a string or number.");
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options) => writer.WriteStringValue(value);
}
