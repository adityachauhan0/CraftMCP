using System.Text.Json;
using System.Text.Json.Serialization;

namespace CraftMCP.Domain.Ids;

[JsonConverter(typeof(NodeIdJsonConverter))]
public readonly record struct NodeId
{
    public NodeId(string value)
    {
        Value = Validate(value, "node_");
    }

    public string Value { get; }

    public static NodeId From(string value) => new(value);

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

    private sealed class NodeIdJsonConverter : JsonConverter<NodeId>
    {
        public override NodeId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            From(reader.GetString() ?? throw new JsonException("Node ID cannot be null."));

        public override void Write(Utf8JsonWriter writer, NodeId value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);

        public override NodeId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            From(reader.GetString() ?? throw new JsonException("Node ID key cannot be null."));

        public override void WriteAsPropertyName(Utf8JsonWriter writer, NodeId value, JsonSerializerOptions options) =>
            writer.WritePropertyName(value.Value);
    }
}
