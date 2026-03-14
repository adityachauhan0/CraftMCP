using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Nodes;

namespace CraftMCP.Domain.Validation;

public static class DocumentValidator
{
    public static DocumentValidationResult Validate(DocumentState document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var errors = new List<DocumentValidationError>();
        var placements = new Dictionary<NodeId, int>();
        var rootSet = new HashSet<NodeId>();

        foreach (var rootId in document.RootNodeIds)
        {
            AddPlacement(placements, rootId);

            if (!document.Nodes.TryGetValue(rootId, out var rootNode))
            {
                AddError(errors, "root_missing", $"Root node '{rootId}' does not exist.");
                continue;
            }

            rootSet.Add(rootId);

            if (rootNode.ParentId is not null)
            {
                AddError(errors, "root_has_parent", $"Root node '{rootId}' cannot have a parent.");
            }
        }

        foreach (var node in document.Nodes.Values)
        {
            if (node.ParentId is null)
            {
                if (!rootSet.Contains(node.Id))
                {
                    AddError(errors, "root_order_missing", $"Node '{node.Id}' has no parent and must appear in root order.");
                }
            }
            else if (!document.Nodes.TryGetValue(node.ParentId.Value, out var parent))
            {
                AddError(errors, "parent_missing", $"Node '{node.Id}' references missing parent '{node.ParentId.Value}'.");
            }
            else if (parent is not GroupNode groupParent)
            {
                AddError(errors, "parent_not_group", $"Node '{node.Id}' parent '{parent.Id}' is not a group.");
            }
            else if (!groupParent.ChildNodeIds.Contains(node.Id))
            {
                AddError(errors, "child_parent_mismatch", $"Node '{node.Id}' parent '{parent.Id}' does not list it as a child.");
            }

            if (node is GroupNode group)
            {
                foreach (var childId in group.ChildNodeIds)
                {
                    AddPlacement(placements, childId);

                    if (!document.Nodes.TryGetValue(childId, out var child))
                    {
                        AddError(errors, "child_missing", $"Group '{group.Id}' references missing child '{childId}'.");
                        continue;
                    }

                    if (child.ParentId != group.Id)
                    {
                        AddError(errors, "child_parent_mismatch", $"Child '{childId}' must reference parent '{group.Id}'.");
                    }
                }
            }

            if (node is ImageNode image && !document.Assets.ContainsKey(image.Asset.AssetId))
            {
                AddError(errors, "asset_missing", $"Image node '{image.Id}' references missing asset '{image.Asset.AssetId}'.");
            }
        }

        foreach (var placement in placements.Where(item => item.Value > 1))
        {
            AddError(errors, "node_referenced_multiple_times", $"Node '{placement.Key}' is placed multiple times in the hierarchy.");
        }

        DetectCycles(document, errors);

        return new DocumentValidationResult(errors);
    }

    private static void DetectCycles(DocumentState document, ICollection<DocumentValidationError> errors)
    {
        var state = new Dictionary<NodeId, VisitState>();

        foreach (var nodeId in document.Nodes.Keys)
        {
            Visit(nodeId, document, state, errors);
        }
    }

    private static void Visit(
        NodeId nodeId,
        DocumentState document,
        IDictionary<NodeId, VisitState> state,
        ICollection<DocumentValidationError> errors)
    {
        if (state.TryGetValue(nodeId, out var currentState))
        {
            if (currentState == VisitState.Visiting)
            {
                AddError(errors, "cycle_detected", $"Cycle detected at node '{nodeId}'.");
            }

            return;
        }

        state[nodeId] = VisitState.Visiting;

        if (document.Nodes.TryGetValue(nodeId, out var node) && node is GroupNode group)
        {
            foreach (var childId in group.ChildNodeIds.Where(document.Nodes.ContainsKey))
            {
                Visit(childId, document, state, errors);
            }
        }

        state[nodeId] = VisitState.Visited;
    }

    private static void AddPlacement(IDictionary<NodeId, int> placements, NodeId nodeId)
    {
        placements[nodeId] = placements.TryGetValue(nodeId, out var count) ? count + 1 : 1;
    }

    private static void AddError(ICollection<DocumentValidationError> errors, string code, string message)
    {
        errors.Add(new DocumentValidationError(code, message));
    }

    private enum VisitState
    {
        Visiting,
        Visited,
    }
}
