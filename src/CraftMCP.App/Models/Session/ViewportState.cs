namespace CraftMCP.App.Models.Session;

public sealed record ViewportState(double Zoom, double PanX, double PanY, bool IsInitialized)
{
    public static ViewportState Default { get; } = new(1d, 0d, 0d, false);
}
