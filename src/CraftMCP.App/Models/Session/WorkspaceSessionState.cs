using CraftMCP.Domain.Ids;

namespace CraftMCP.App.Models.Session;

public sealed record WorkspaceSessionState(
    SelectionState Selection,
    NodeId? HoverNodeId,
    ToolMode ToolMode,
    ViewportState Viewport,
    bool IsLayerPanelOpen,
    bool IsPropertyPanelOpen,
    bool IsPromptPanelOpen,
    bool IsActivityPanelOpen)
{
    public static WorkspaceSessionState Default { get; } =
        new(
            SelectionState.Empty,
            null,
            ToolMode.Select,
            ViewportState.Default,
            true,
            true,
            true,
            true);
}
