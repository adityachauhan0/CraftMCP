using System.Text.Json.Serialization;

namespace CraftMCP.Domain.ValueObjects;

public readonly record struct SizeValue
{
    [JsonConstructor]
    public SizeValue(double width, double height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
        }

        Width = width;
        Height = height;
    }

    public double Width { get; }

    public double Height { get; }
}
