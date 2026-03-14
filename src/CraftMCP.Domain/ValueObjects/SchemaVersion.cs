using System.Text.Json;
using System.Text.Json.Serialization;

namespace CraftMCP.Domain.ValueObjects;

[JsonConverter(typeof(SchemaVersionJsonConverter))]
public readonly record struct SchemaVersion
{
    public SchemaVersion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Schema version cannot be blank.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public static SchemaVersion V1 => new("v1");

    public override string ToString() => Value;

    private sealed class SchemaVersionJsonConverter : JsonConverter<SchemaVersion>
    {
        public override SchemaVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString() ?? throw new JsonException("Schema version cannot be null."));

        public override void Write(Utf8JsonWriter writer, SchemaVersion value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);
    }
}
