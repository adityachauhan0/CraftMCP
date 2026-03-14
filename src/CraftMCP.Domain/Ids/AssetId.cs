using System.Text.Json;
using System.Text.Json.Serialization;

namespace CraftMCP.Domain.Ids;

[JsonConverter(typeof(AssetIdJsonConverter))]
public readonly record struct AssetId
{
    public AssetId(string value)
    {
        Value = Validate(value, "asset_");
    }

    public string Value { get; }

    public static AssetId From(string value) => new(value);

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

    private sealed class AssetIdJsonConverter : JsonConverter<AssetId>
    {
        public override AssetId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            From(reader.GetString() ?? throw new JsonException("Asset ID cannot be null."));

        public override void Write(Utf8JsonWriter writer, AssetId value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);

        public override AssetId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            From(reader.GetString() ?? throw new JsonException("Asset ID key cannot be null."));

        public override void WriteAsPropertyName(Utf8JsonWriter writer, AssetId value, JsonSerializerOptions options) =>
            writer.WritePropertyName(value.Value);
    }
}
