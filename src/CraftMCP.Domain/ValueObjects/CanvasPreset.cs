using System.Text.Json;
using System.Text.Json.Serialization;

namespace CraftMCP.Domain.ValueObjects;

[JsonConverter(typeof(CanvasPresetJsonConverter))]
public readonly record struct CanvasPreset
{
    public CanvasPreset(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Canvas preset cannot be blank.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public static CanvasPreset Custom => new("custom");

    public static CanvasPreset SquarePost => new("square-post");

    public static CanvasPreset Slide => new("slide");

    public static CanvasPreset DesktopFrame => new("desktop-frame");

    public static CanvasPreset MobileFrame => new("mobile-frame");

    public override string ToString() => Value;

    private sealed class CanvasPresetJsonConverter : JsonConverter<CanvasPreset>
    {
        public override CanvasPreset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString() ?? throw new JsonException("Canvas preset cannot be null."));

        public override void Write(Utf8JsonWriter writer, CanvasPreset value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);
    }
}
