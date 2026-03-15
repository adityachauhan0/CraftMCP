using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Nodes;

namespace CraftMCP.Domain.Commands;

internal sealed record CommandProcessingResult(
    bool IsSuccess,
    DocumentState? Document,
    IReadOnlyList<CommandWarning> Warnings,
    IReadOnlyList<CommandFailure> Errors,
    IReadOnlyList<NodeId> AffectedNodeIds,
    IReadOnlyList<AssetId> AffectedAssetIds,
    IReadOnlyList<DesignCommand> InverseCommands)
{
    public static CommandProcessingResult Success(
        DocumentState document,
        IReadOnlyList<CommandWarning> warnings,
        IReadOnlyList<NodeId> affectedNodeIds,
        IReadOnlyList<AssetId> affectedAssetIds,
        IReadOnlyList<DesignCommand> inverseCommands) =>
        new(true, document, warnings, Array.Empty<CommandFailure>(), affectedNodeIds, affectedAssetIds, inverseCommands);

    public static CommandProcessingResult Failure(
        IReadOnlyList<CommandWarning> warnings,
        IReadOnlyList<CommandFailure> errors) =>
        new(false, null, warnings, errors, Array.Empty<NodeId>(), Array.Empty<AssetId>(), Array.Empty<DesignCommand>());
}

internal sealed class MutableDocumentState
{
    private MutableDocumentState(
        CanvasModel canvas,
        Dictionary<NodeId, NodeBase> nodes,
        List<NodeId> rootNodeIds,
        Dictionary<AssetId, AssetManifestEntry> assets)
    {
        Canvas = canvas;
        Nodes = nodes;
        RootNodeIds = rootNodeIds;
        Assets = assets;
    }

    public CanvasModel Canvas { get; set; }

    public Dictionary<NodeId, NodeBase> Nodes { get; }

    public List<NodeId> RootNodeIds { get; }

    public Dictionary<AssetId, AssetManifestEntry> Assets { get; }

    public static MutableDocumentState From(DocumentState document) =>
        new(
            document.Canvas,
            document.Nodes.ToDictionary(entry => entry.Key, entry => entry.Value),
            document.RootNodeIds.ToList(),
            document.Assets.ToDictionary(entry => entry.Key, entry => entry.Value));

    public DocumentState ToDocumentState(DocumentState source) =>
        new(
            source.Id,
            source.SchemaVersion,
            source.Name,
            Canvas,
            Nodes.ToDictionary(entry => entry.Key, entry => entry.Value),
            RootNodeIds.ToArray(),
            Assets.ToDictionary(entry => entry.Key, entry => entry.Value));

    public IReadOnlyList<NodeId> GetSiblingIds(NodeId? parentId) =>
        parentId is null
            ? RootNodeIds
            : ((GroupNode)Nodes[parentId.Value]).ChildNodeIds.ToArray();

    public int GetInsertIndex(NodeId nodeId)
    {
        var node = Nodes[nodeId];
        var siblings = node.ParentId is null
            ? RootNodeIds
            : ((GroupNode)Nodes[node.ParentId.Value]).ChildNodeIds;
        for (var index = 0; index < siblings.Count; index++)
        {
            if (siblings[index] == nodeId)
            {
                return index;
            }
        }

        throw new InvalidOperationException($"Node '{nodeId}' does not have a sibling placement.");
    }

    public void InsertNode(NodeBase node, int insertIndex)
    {
        Nodes[node.Id] = node;
        InsertPlacement(node.Id, node.ParentId, insertIndex);
    }

    public void InsertPlacement(NodeId nodeId, NodeId? parentId, int insertIndex)
    {
        if (parentId is null)
        {
            RootNodeIds.Insert(insertIndex, nodeId);
            return;
        }

        var group = (GroupNode)Nodes[parentId.Value];
        var childIds = group.ChildNodeIds.ToList();
        if (!childIds.Contains(nodeId))
        {
            childIds.Insert(insertIndex, nodeId);
            Nodes[parentId.Value] = group with { ChildNodeIds = childIds };
        }
    }

    public void InsertPlacements(IEnumerable<NodeId> nodeIds, NodeId? parentId, int insertIndex)
    {
        if (parentId is null)
        {
            RootNodeIds.InsertRange(insertIndex, nodeIds);
            return;
        }

        var group = (GroupNode)Nodes[parentId.Value];
        var childIds = group.ChildNodeIds.ToList();
        childIds.InsertRange(insertIndex, nodeIds);
        Nodes[parentId.Value] = group with { ChildNodeIds = childIds };
    }

    public void RemovePlacement(NodeId nodeId)
    {
        var node = Nodes[nodeId];
        if (node.ParentId is null)
        {
            RootNodeIds.Remove(nodeId);
            return;
        }

        var group = (GroupNode)Nodes[node.ParentId.Value];
        Nodes[node.ParentId.Value] = group with { ChildNodeIds = group.ChildNodeIds.Where(childId => childId != nodeId).ToArray() };
    }

    public IEnumerable<NodeId> EnumerateSubtree(NodeId rootNodeId)
    {
        yield return rootNodeId;

        if (Nodes[rootNodeId] is not GroupNode group)
        {
            yield break;
        }

        foreach (var childId in group.ChildNodeIds)
        {
            foreach (var descendantId in EnumerateSubtree(childId))
            {
                yield return descendantId;
            }
        }
    }

    public bool IsDescendantOf(NodeId? candidateParentId, NodeId nodeId)
    {
        var currentParent = candidateParentId;
        while (currentParent is not null)
        {
            if (currentParent.Value == nodeId)
            {
                return true;
            }

            currentParent = Nodes.TryGetValue(currentParent.Value, out var currentNode)
                ? currentNode.ParentId
                : null;
        }

        return false;
    }

    public void SetChildNodeIds(NodeId groupId, IReadOnlyList<NodeId> childNodeIds)
    {
        var group = (GroupNode)Nodes[groupId];
        Nodes[groupId] = group with { ChildNodeIds = childNodeIds.ToArray() };
    }
}

internal sealed class OrderedIdSet<T>
    where T : notnull
{
    private readonly List<T> items = new();
    private readonly HashSet<T> seen = new();

    public IReadOnlyList<T> Items => items;

    public void Add(T item)
    {
        if (seen.Add(item))
        {
            items.Add(item);
        }
    }
}
