using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Nodes;

namespace CraftMCP.Agent;

public static class SceneContextFactory
{
    public static SceneContext Create(
        DocumentState document,
        string prompt,
        IReadOnlyList<NodeId> selectedNodeIds)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(selectedNodeIds);

        var nodeSummaries = document.Nodes.Values
            .Select(CreateNodeSummary)
            .ToDictionary(summary => summary.NodeId, summary => summary);

        var selectedNodes = selectedNodeIds
            .Where(nodeSummaries.ContainsKey)
            .Select(nodeId => nodeSummaries[nodeId])
            .ToArray();

        var hierarchy = document.RootNodeIds
            .Where(document.Nodes.ContainsKey)
            .Select(nodeId => CreateHierarchySummary(document.Nodes[nodeId]))
            .ToArray();

        return new SceneContext(
            prompt,
            new SceneCanvasContext(
                document.Canvas.Width,
                document.Canvas.Height,
                document.Canvas.Preset,
                document.Canvas.Background,
                document.Canvas.SafeArea),
            new SelectedNodeContext(
                selectedNodeIds.ToArray(),
                selectedNodeIds.Count == 0 ? null : selectedNodeIds[^1],
                selectedNodes),
            hierarchy,
            nodeSummaries.Values.ToArray());
    }

    private static HierarchyNodeSummary CreateHierarchySummary(NodeBase node) =>
        new(
            node.Id,
            node.Name,
            node.Kind,
            node.ParentId,
            node is GroupNode group ? group.ChildNodeIds.ToArray() : Array.Empty<NodeId>());

    private static SceneNodeSummary CreateNodeSummary(NodeBase node) =>
        node switch
        {
            RectangleNode rectangle => new SceneNodeSummary(
                rectangle.Id,
                rectangle.Name,
                rectangle.Kind,
                rectangle.ParentId,
                rectangle.Transform,
                rectangle.IsVisible,
                rectangle.IsLocked,
                rectangle.Opacity,
                rectangle.Size,
                null,
                null,
                null,
                rectangle.Fill,
                rectangle.Stroke,
                rectangle.CornerRadius,
                null,
                null,
                null,
                null,
                Array.Empty<NodeId>()),
            CircleNode circle => new SceneNodeSummary(
                circle.Id,
                circle.Name,
                circle.Kind,
                circle.ParentId,
                circle.Transform,
                circle.IsVisible,
                circle.IsLocked,
                circle.Opacity,
                circle.Size,
                null,
                null,
                null,
                circle.Fill,
                circle.Stroke,
                null,
                null,
                null,
                null,
                null,
                Array.Empty<NodeId>()),
            TextNode text => new SceneNodeSummary(
                text.Id,
                text.Name,
                text.Kind,
                text.ParentId,
                text.Transform,
                text.IsVisible,
                text.IsLocked,
                text.Opacity,
                null,
                text.Bounds,
                null,
                null,
                text.Fill,
                null,
                null,
                text.Content,
                text.Typography,
                null,
                null,
                Array.Empty<NodeId>()),
            ImageNode image => new SceneNodeSummary(
                image.Id,
                image.Name,
                image.Kind,
                image.ParentId,
                image.Transform,
                image.IsVisible,
                image.IsLocked,
                image.Opacity,
                null,
                image.Bounds,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                image.Asset.AssetId,
                image.FitMode,
                Array.Empty<NodeId>()),
            LineNode line => new SceneNodeSummary(
                line.Id,
                line.Name,
                line.Kind,
                line.ParentId,
                line.Transform,
                line.IsVisible,
                line.IsLocked,
                line.Opacity,
                null,
                null,
                line.Start,
                line.End,
                null,
                line.Stroke,
                null,
                null,
                null,
                null,
                null,
                Array.Empty<NodeId>()),
            GroupNode group => new SceneNodeSummary(
                group.Id,
                group.Name,
                group.Kind,
                group.ParentId,
                group.Transform,
                group.IsVisible,
                group.IsLocked,
                group.Opacity,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                group.ChildNodeIds.ToArray()),
            _ => throw new NotSupportedException($"Unsupported node kind '{node.Kind}'."),
        };
}
