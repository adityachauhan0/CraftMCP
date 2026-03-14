using System.Text.Json;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Serialization;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Tests.Unit.Models;

public sealed class CanvasModelTests
{
    [Fact]
    public void Constructor_RejectsNonPositiveDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CanvasModel(0, 1080, CanvasPreset.Custom, new ColorValue(255, 255, 255)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CanvasModel(1920, -1, CanvasPreset.Custom, new ColorValue(255, 255, 255)));
    }

    [Fact]
    public void CanvasModel_RoundTripsWithPresetAndSafeArea()
    {
        var original = new CanvasModel(
            1080,
            1080,
            CanvasPreset.SquarePost,
            new ColorValue(240, 240, 240),
            new SafeAreaInsets(24, 24, 24, 24));

        var json = JsonSerializer.Serialize(original, CraftJsonSerializerOptions.Default);
        var restored = JsonSerializer.Deserialize<CanvasModel>(json, CraftJsonSerializerOptions.Default);

        Assert.NotNull(restored);
        Assert.Equal(original, restored);
    }
}
