using System.Text.Json.Serialization;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Domain.Models;

public sealed record CanvasModel
{
    [JsonConstructor]
    public CanvasModel(double width, double height, CanvasPreset preset, ColorValue background, SafeAreaInsets? safeArea = null)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Canvas width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Canvas height must be greater than zero.");
        }

        Width = width;
        Height = height;
        Preset = preset;
        Background = background;
        SafeArea = safeArea;
    }

    public double Width { get; init; }

    public double Height { get; init; }

    public CanvasPreset Preset { get; init; }

    public ColorValue Background { get; init; }

    public SafeAreaInsets? SafeArea { get; init; }
}
