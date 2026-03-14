namespace CraftMCP.Domain.ValueObjects;

public readonly record struct TransformValue(double X, double Y, double ScaleX, double ScaleY, double RotationDegrees)
{
    public static TransformValue Identity => new(0, 0, 1, 1, 0);
}
