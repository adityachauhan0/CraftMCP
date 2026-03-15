using CraftMCP.Domain.Ids;

namespace CraftMCP.Rendering.Scene;

public sealed record RenderOverlayState(
    IReadOnlyCollection<NodeId> SelectedNodeIds,
    bool ShowSafeAreaGuides = false)
{
    public static RenderOverlayState Empty { get; } =
        new(Array.Empty<NodeId>(), false);
}
