using System.Text.Json.Serialization;

namespace CraftMCP.Domain.ValueObjects;

public readonly record struct OpacityValue
{
    [JsonConstructor]
    public OpacityValue(double value)
    {
        if (value < 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Opacity must be between 0 and 1.");
        }

        Value = value;
    }

    public double Value { get; }

    public static OpacityValue Full => new(1);
}
