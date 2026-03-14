using System.Text.Json.Serialization;

namespace CraftMCP.Domain.ValueObjects;

public readonly record struct SafeAreaInsets
{
    [JsonConstructor]
    public SafeAreaInsets(double left, double top, double right, double bottom)
    {
        if (left < 0 || top < 0 || right < 0 || bottom < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(left), "Safe-area values cannot be negative.");
        }

        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public double Left { get; }

    public double Top { get; }

    public double Right { get; }

    public double Bottom { get; }
}
