namespace CraftMCP.Rendering.Scene;

public readonly record struct AxisAlignedBounds
{
    public AxisAlignedBounds(double left, double top, double right, double bottom)
    {
        if (right < left)
        {
            throw new ArgumentOutOfRangeException(nameof(right), "Right cannot be less than left.");
        }

        if (bottom < top)
        {
            throw new ArgumentOutOfRangeException(nameof(bottom), "Bottom cannot be less than top.");
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

    public double Width => Right - Left;

    public double Height => Bottom - Top;

    public static AxisAlignedBounds Union(AxisAlignedBounds left, AxisAlignedBounds right) =>
        new(
            Math.Min(left.Left, right.Left),
            Math.Min(left.Top, right.Top),
            Math.Max(left.Right, right.Right),
            Math.Max(left.Bottom, right.Bottom));
}
