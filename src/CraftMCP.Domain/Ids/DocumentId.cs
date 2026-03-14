using System.Text.Json;
using System.Text.Json.Serialization;

namespace CraftMCP.Domain.Ids;

[JsonConverter(typeof(DocumentIdJsonConverter))]
public readonly record struct DocumentId
{
    public DocumentId(string value)
    {
        Value = Validate(value, "doc_");
    }

    public string Value { get; }

    public static DocumentId From(string value) => new(value);

    public override string ToString() => Value;

    private static string Validate(string value, string prefix)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ID value cannot be blank.", nameof(value));
        }

        if (!value.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new ArgumentException($"ID value must start with '{prefix}'.", nameof(value));
        }

        return value;
    }

    private sealed class DocumentIdJsonConverter : JsonConverter<DocumentId>
    {
        public override DocumentId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            From(reader.GetString() ?? throw new JsonException("Document ID cannot be null."));

        public override void Write(Utf8JsonWriter writer, DocumentId value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);

        public override DocumentId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            From(reader.GetString() ?? throw new JsonException("Document ID key cannot be null."));

        public override void WriteAsPropertyName(Utf8JsonWriter writer, DocumentId value, JsonSerializerOptions options) =>
            writer.WritePropertyName(value.Value);
    }
}
