using CraftMCP.Domain.Ids;

namespace CraftMCP.App.Models.Session;

public sealed record SelectionState
{
    public static SelectionState Empty { get; } = new(Array.Empty<NodeId>());

    public SelectionState(IReadOnlyList<NodeId> selectedNodeIds)
    {
        SelectedNodeIds = selectedNodeIds ?? throw new ArgumentNullException(nameof(selectedNodeIds));
    }

    public IReadOnlyList<NodeId> SelectedNodeIds { get; init; }

    public bool HasSelection => SelectedNodeIds.Count > 0;

    public bool HasSingleSelection => SelectedNodeIds.Count == 1;

    public bool HasMultipleSelection => SelectedNodeIds.Count > 1;

    public NodeId? PrimaryNodeId => HasSelection ? SelectedNodeIds[^1] : null;
}
