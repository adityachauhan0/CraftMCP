using System.Text.Json.Serialization;

namespace CraftMCP.Domain.ValueObjects;

public readonly record struct StrokeStyle
{
    [JsonConstructor]
    public StrokeStyle(ColorValue color, double width)
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Stroke width cannot be negative.");
        }

        Color = color;
        Width = width;
    }

    public ColorValue Color { get; }

    public double Width { get; }
}
