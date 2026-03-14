using System.Text.Json;
using CraftMCP.Domain.Serialization;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Tests.Unit.ValueObjects;

public sealed class ValueObjectSerializationTests
{
    [Fact]
    public void ColorValue_SerializesWithChannels()
    {
        var color = new ColorValue(12, 34, 56, 78);
        var json = JsonSerializer.Serialize(color, CraftJsonSerializerOptions.Default);

        Assert.Equal("{\"red\":12,\"green\":34,\"blue\":56,\"alpha\":78}", json);
    }

    [Fact]
    public void OpacityValue_RejectsOutOfRangeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new OpacityValue(-0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OpacityValue(1.1));
    }

    [Fact]
    public void TypographyStyle_RoundTripsThroughJson()
    {
        var original = new TypographyStyle("Inter", 32, 600, "center", 1.2, 0.4);
        var json = JsonSerializer.Serialize(original, CraftJsonSerializerOptions.Default);
        var restored = JsonSerializer.Deserialize<TypographyStyle>(json, CraftJsonSerializerOptions.Default);

        Assert.Equal(original, restored);
    }
}
