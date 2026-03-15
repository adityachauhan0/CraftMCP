using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.Validation;

namespace CraftMCP.Domain.Commands;

internal static class CommandEngineCore
{
    public static CommandProcessingResult Process(DocumentState document, CommandBatch batch, bool includeInverses)
    {
        var working = MutableDocumentState.From(document);
        var warnings = new List<CommandWarning>();
        var errors = new List<CommandFailure>();
        var affectedNodeIds = new OrderedIdSet<NodeId>();
        var affectedAssetIds = new OrderedIdSet<AssetId>();
        var inversePerCommand = new List<IReadOnlyList<DesignCommand>>(batch.Commands.Count);

        for (var commandIndex = 0; commandIndex < batch.Commands.Count; commandIndex++)
        {
            var command = batch.Commands[commandIndex];
            if (!TryApplyCommand(
                    working,
                    command,
                    commandIndex,
                    warnings,
                    errors,
                    affectedNodeIds,
                    affectedAssetIds,
                    out var inverseCommands))
            {
                return CommandProcessingResult.Failure(warnings, errors);
            }

            inversePerCommand.Add(includeInverses ? inverseCommands : Array.Empty<DesignCommand>());
        }

        var candidate = working.ToDocumentState(document);
        var documentValidation = DocumentValidator.Validate(candidate);
        if (!documentValidation.IsValid)
        {
            errors.AddRange(documentValidation.Errors.Select(error => new CommandFailure(error.Code, error.Message)));
            return CommandProcessingResult.Failure(warnings, errors);
        }

        var flattenedInverseCommands = includeInverses
            ? inversePerCommand
                .AsEnumerable()
                .Reverse()
                .SelectMany(commands => commands)
                .ToArray()
            : Array.Empty<DesignCommand>();

        return CommandProcessingResult.Success(candidate, warnings, affectedNodeIds.Items, affectedAssetIds.Items, flattenedInverseCommands);
    }

    private static bool TryApplyCommand(
        MutableDocumentState working,
        DesignCommand command,
        int commandIndex,
        ICollection<CommandWarning> warnings,
        ICollection<CommandFailure> errors,
        OrderedIdSet<NodeId> affectedNodeIds,
        OrderedIdSet<AssetId> affectedAssetIds,
        out IReadOnlyList<DesignCommand> inverseCommands) =>
        command switch
        {
            CreateNodeCommand createNode => TryCreateNode(working, createNode, commandIndex, errors, affectedNodeIds, out inverseCommands),
            UpdateNodeCommand updateNode => TryUpdateNode(working, updateNode, commandIndex, errors, affectedNodeIds, out inverseCommands),
            DeleteNodeCommand deleteNode => TryDeleteNode(working, deleteNode, commandIndex, errors, affectedNodeIds, out inverseCommands),
            ReorderNodeCommand reorderNode => TryReorderNode(working, reorderNode, commandIndex, errors, affectedNodeIds, out inverseCommands),
            GroupNodesCommand groupNodes => TryGroupNodes(working, groupNodes, commandIndex, errors, affectedNodeIds, out inverseCommands),
            UngroupNodeCommand ungroupNode => TryUngroupNode(working, ungroupNode, commandIndex, errors, affectedNodeIds, out inverseCommands),
            SetCanvasCommand setCanvas => TrySetCanvas(working, setCanvas, out inverseCommands),
            ImportAssetCommand importAsset => TryImportAsset(working, importAsset, commandIndex, warnings, errors, affectedAssetIds, out inverseCommands),
            RemoveAssetCommand removeAsset => TryRemoveAsset(working, removeAsset, commandIndex, errors, affectedAssetIds, out inverseCommands),
            DuplicateNodeCommand duplicateNode => TryDuplicateNode(working, duplicateNode, commandIndex, errors, affectedNodeIds, out inverseCommands),
            SetVisibilityCommand setVisibility => TrySetVisibility(working, setVisibility, commandIndex, errors, affectedNodeIds, out inverseCommands),
            SetLockStateCommand setLockState => TrySetLockState(working, setLockState, commandIndex, errors, affectedNodeIds, out inverseCommands),
            _ => FailCommand(errors, "command_not_supported", $"Command '{command.Kind}' is not supported.", commandIndex, out inverseCommands),
        };

    private static bool TryCreateNode(
        MutableDocumentState working,
        CreateNodeCommand command,
        int commandIndex,
        ICollection<CommandFailure> errors,
        OrderedIdSet<NodeId> affectedNodeIds,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        if (working.Nodes.ContainsKey(command.Node.Id))
        {
            return FailNode(errors, "node_already_exists", $"Node '{command.Node.Id}' already exists.", commandIndex, command.Node.Id, out inverseCommands);
        }

        if (!ValidateParent(working, command.Node.ParentId, commandIndex, errors, command.Node.Id, out inverseCommands)
            || !ValidateInsertIndex(working, command.Node.ParentId, command.InsertIndex, commandIndex, errors, command.Node.Id, out inverseCommands))
        {
            return false;
        }

        if (command.Node is ImageNode image && !working.Assets.ContainsKey(image.Asset.AssetId))
        {
            return FailAsset(errors, "asset_not_found", $"Asset '{image.Asset.AssetId}' does not exist.", commandIndex, command.Node.Id, image.Asset.AssetId, out inverseCommands);
        }

        working.Nodes[command.Node.Id] = command.Node;
        working.InsertPlacement(command.Node.Id, command.Node.ParentId, command.InsertIndex);
        affectedNodeIds.Add(command.Node.Id);
        if (command.Node.ParentId is { } parentId)
        {
            affectedNodeIds.Add(parentId);
        }

        inverseCommands = new DesignCommand[] { new DeleteNodeCommand(command.Node.Id) };
        return true;
    }

    private static bool TryUpdateNode(
        MutableDocumentState working,
        UpdateNodeCommand command,
        int commandIndex,
        ICollection<CommandFailure> errors,
        OrderedIdSet<NodeId> affectedNodeIds,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        if (!working.Nodes.TryGetValue(command.Node.Id, out var existingNode))
        {
            return FailNode(errors, "node_not_found", $"Node '{command.Node.Id}' does not exist.", commandIndex, command.Node.Id, out inverseCommands);
        }

        if (existingNode.Kind != command.Node.Kind)
        {
            return FailNode(errors, "node_kind_mismatch", $"Node '{command.Node.Id}' cannot change kind from '{existingNode.Kind}' to '{command.Node.Kind}'.", commandIndex, command.Node.Id, out inverseCommands);
        }

        if (existingNode.ParentId != command.Node.ParentId)
        {
            return FailNode(errors, "node_parent_change_not_allowed", $"Node '{command.Node.Id}' cannot change parent through update.", commandIndex, command.Node.Id, out inverseCommands);
        }

        if (existingNode is GroupNode existingGroup
            && command.Node is GroupNode updatedGroup
            && !existingGroup.ChildNodeIds.SequenceEqual(updatedGroup.ChildNodeIds))
        {
            return FailNode(errors, "group_children_change_not_allowed", $"Group '{command.Node.Id}' cannot change child order through update.", commandIndex, command.Node.Id, out inverseCommands);
        }

        if (command.Node is ImageNode image && !working.Assets.ContainsKey(image.Asset.AssetId))
        {
            return FailAsset(errors, "asset_not_found", $"Asset '{image.Asset.AssetId}' does not exist.", commandIndex, command.Node.Id, image.Asset.AssetId, out inverseCommands);
        }

        working.Nodes[command.Node.Id] = command.Node;
        affectedNodeIds.Add(command.Node.Id);
        inverseCommands = new DesignCommand[] { new UpdateNodeCommand(existingNode) };
        return true;
    }

    private static bool TryDeleteNode(
        MutableDocumentState working,
        DeleteNodeCommand command,
        int commandIndex,
        ICollection<CommandFailure> errors,
        OrderedIdSet<NodeId> affectedNodeIds,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        if (!working.Nodes.TryGetValue(command.NodeId, out var existingNode))
        {
            return FailNode(errors, "node_not_found", $"Node '{command.NodeId}' does not exist.", commandIndex, command.NodeId, out inverseCommands);
        }

        var subtreeIds = working.EnumerateSubtree(command.NodeId).ToArray();
        var createCommands = new List<DesignCommand>(subtreeIds.Length);

        foreach (var nodeId in subtreeIds)
        {
            createCommands.Add(new CreateNodeCommand(working.Nodes[nodeId], working.GetInsertIndex(nodeId)));
        }

        foreach (var nodeId in subtreeIds.Reverse())
        {
            if (working.Nodes[nodeId].ParentId is { } parentId)
            {
                affectedNodeIds.Add(parentId);
            }

            working.RemovePlacement(nodeId);
            working.Nodes.Remove(nodeId);
            affectedNodeIds.Add(nodeId);
        }

        if (existingNode.ParentId is null)
        {
            affectedNodeIds.Add(command.NodeId);
        }

        inverseCommands = createCommands;
        return true;
    }

    private static bool TryReorderNode(
        MutableDocumentState working,
        ReorderNodeCommand command,
        int commandIndex,
        ICollection<CommandFailure> errors,
        OrderedIdSet<NodeId> affectedNodeIds,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        if (!working.Nodes.TryGetValue(command.NodeId, out var existingNode))
        {
            return FailNode(errors, "node_not_found", $"Node '{command.NodeId}' does not exist.", commandIndex, command.NodeId, out inverseCommands);
        }

        if (!ValidateParent(working, command.NewParentId, commandIndex, errors, command.NodeId, out inverseCommands))
        {
            return false;
        }

        if (command.NewParentId == command.NodeId || (command.NewParentId is not null && working.IsDescendantOf(command.NewParentId, command.NodeId)))
        {
            return FailNode(errors, "hierarchy_cycle_not_allowed", $"Node '{command.NodeId}' cannot be moved under itself or its descendants.", commandIndex, command.NodeId, out inverseCommands);
        }

        var oldParentId = existingNode.ParentId;
        var oldIndex = working.GetInsertIndex(command.NodeId);

        working.RemovePlacement(command.NodeId);
        if (!ValidateInsertIndex(working, command.NewParentId, command.InsertIndex, commandIndex, errors, command.NodeId, out inverseCommands))
        {
            working.InsertPlacement(command.NodeId, oldParentId, oldIndex);
            return false;
        }

        working.Nodes[command.NodeId] = existingNode with { ParentId = command.NewParentId };
        working.InsertPlacement(command.NodeId, command.NewParentId, command.InsertIndex);

        affectedNodeIds.Add(command.NodeId);
        if (oldParentId is { } oldParent)
        {
            affectedNodeIds.Add(oldParent);
        }

        if (command.NewParentId is { } newParent)
        {
            affectedNodeIds.Add(newParent);
        }

        inverseCommands = new DesignCommand[] { new ReorderNodeCommand(command.NodeId, oldParentId, oldIndex) };
        return true;
    }

    private static bool TryGroupNodes(
        MutableDocumentState working,
        GroupNodesCommand command,
        int commandIndex,
        ICollection<CommandFailure> errors,
        OrderedIdSet<NodeId> affectedNodeIds,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        if (working.Nodes.ContainsKey(command.Group.Id))
        {
            return FailNode(errors, "node_already_exists", $"Node '{command.Group.Id}' already exists.", commandIndex, command.Group.Id, out inverseCommands);
        }

        if (command.Group.ChildNodeIds.Count == 0)
        {
            return FailNode(errors, "group_children_required", $"Group '{command.Group.Id}' must contain at least one child.", commandIndex, command.Group.Id, out inverseCommands);
        }

        if (command.Group.ChildNodeIds.Distinct().Count() != command.Group.ChildNodeIds.Count)
        {
            return FailNode(errors, "group_children_duplicate", $"Group '{command.Group.Id}' cannot contain duplicate child IDs.", commandIndex, command.Group.Id, out inverseCommands);
        }

        if (!ValidateParent(working, command.Group.ParentId, commandIndex, errors, command.Group.Id, out inverseCommands))
        {
            return false;
        }

        NodeId? sharedParentId = null;
        var childIndexes = new List<int>(command.Group.ChildNodeIds.Count);

        foreach (var childId in command.Group.ChildNodeIds)
        {
            if (!working.Nodes.TryGetValue(childId, out var childNode))
            {
                return FailNode(errors, "node_not_found", $"Node '{childId}' does not exist.", commandIndex, childId, out inverseCommands);
            }

            sharedParentId ??= childNode.ParentId;
            if (sharedParentId != childNode.ParentId || childNode.ParentId != command.Group.ParentId)
            {
                return FailNode(errors, "group_parent_mismatch", $"Group '{command.Group.Id}' can only group nodes that already share its parent.", commandIndex, childId, out inverseCommands);
            }

            childIndexes.Add(working.GetInsertIndex(childId));
        }

        var orderedChildIds = working.GetSiblingIds(command.Group.ParentId)
            .Where(command.Group.ChildNodeIds.Contains)
            .ToArray();
        if (!orderedChildIds.SequenceEqual(command.Group.ChildNodeIds))
        {
            return FailNode(errors, "group_order_mismatch", $"Group '{command.Group.Id}' child order must match the current sibling order.", commandIndex, command.Group.Id, out inverseCommands);
        }

        if (childIndexes.Max() - childIndexes.Min() + 1 != childIndexes.Count)
        {
            return FailNode(errors, "group_children_not_contiguous", $"Group '{command.Group.Id}' can only group contiguous siblings in this phase.", commandIndex, command.Group.Id, out inverseCommands);
        }

        if (command.InsertIndex != childIndexes.Min())
        {
            return FailNode(errors, "group_insert_index_mismatch", $"Group '{command.Group.Id}' must be inserted at the first selected sibling index.", commandIndex, command.Group.Id, out inverseCommands);
        }

        working.InsertNode(command.Group, command.InsertIndex);
        foreach (var childId in command.Group.ChildNodeIds)
        {
            working.RemovePlacement(childId);
            working.Nodes[childId] = working.Nodes[childId] with { ParentId = command.Group.Id };
            affectedNodeIds.Add(childId);
        }

        working.SetChildNodeIds(command.Group.Id, command.Group.ChildNodeIds);
        affectedNodeIds.Add(command.Group.Id);
        inverseCommands = new DesignCommand[] { new UngroupNodeCommand(command.Group.Id) };
        return true;
    }

    private static bool TryUngroupNode(
        MutableDocumentState working,
        UngroupNodeCommand command,
        int commandIndex,
        ICollection<CommandFailure> errors,
        OrderedIdSet<NodeId> affectedNodeIds,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        if (!working.Nodes.TryGetValue(command.GroupId, out var existingNode) || existingNode is not GroupNode group)
        {
            return FailNode(errors, "group_not_found", $"Group '{command.GroupId}' does not exist.", commandIndex, command.GroupId, out inverseCommands);
        }

        var groupIndex = working.GetInsertIndex(command.GroupId);
        working.RemovePlacement(command.GroupId);

        foreach (var childId in group.ChildNodeIds)
        {
            working.Nodes[childId] = working.Nodes[childId] with { ParentId = group.ParentId };
            affectedNodeIds.Add(childId);
        }

        working.InsertPlacements(group.ChildNodeIds, group.ParentId, groupIndex);
        working.Nodes.Remove(command.GroupId);
        affectedNodeIds.Add(command.GroupId);
        if (group.ParentId is { } parentId)
        {
            affectedNodeIds.Add(parentId);
        }

        inverseCommands = new DesignCommand[] { new GroupNodesCommand(group, groupIndex) };
        return true;
    }

    private static bool TrySetCanvas(
        MutableDocumentState working,
        SetCanvasCommand command,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        var previousCanvas = working.Canvas;
        working.Canvas = command.Canvas;
        inverseCommands = new DesignCommand[] { new SetCanvasCommand(previousCanvas) };
        return true;
    }

    private static bool TryImportAsset(
        MutableDocumentState working,
        ImportAssetCommand command,
        int commandIndex,
        ICollection<CommandWarning> warnings,
        ICollection<CommandFailure> errors,
        OrderedIdSet<AssetId> affectedAssetIds,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        if (working.Assets.ContainsKey(command.Asset.Id))
        {
            return FailAsset(errors, "asset_already_exists", $"Asset '{command.Asset.Id}' already exists.", commandIndex, null, command.Asset.Id, out inverseCommands);
        }

        var reusedAsset = working.Assets.Values.FirstOrDefault(asset => asset.ContentHash == command.Asset.ContentHash);
        if (reusedAsset is not null)
        {
            warnings.Add(new CommandWarning(
                "asset_content_hash_reused",
                $"Asset content hash '{command.Asset.ContentHash}' is already packaged as '{reusedAsset.Id}'.",
                commandIndex,
                assetId: reusedAsset.Id));
        }

        working.Assets[command.Asset.Id] = command.Asset;
        affectedAssetIds.Add(command.Asset.Id);
        inverseCommands = new DesignCommand[] { new RemoveAssetCommand(command.Asset.Id) };
        return true;
    }

    private static bool TryRemoveAsset(
        MutableDocumentState working,
        RemoveAssetCommand command,
        int commandIndex,
        ICollection<CommandFailure> errors,
        OrderedIdSet<AssetId> affectedAssetIds,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        if (!working.Assets.TryGetValue(command.AssetId, out var existingAsset))
        {
            return FailAsset(errors, "asset_not_found", $"Asset '{command.AssetId}' does not exist.", commandIndex, null, command.AssetId, out inverseCommands);
        }

        var referencingNode = working.Nodes.Values.OfType<ImageNode>().FirstOrDefault(node => node.Asset.AssetId == command.AssetId);
        if (referencingNode is not null)
        {
            return FailAsset(errors, "asset_in_use", $"Asset '{command.AssetId}' is still referenced by node '{referencingNode.Id}'.", commandIndex, referencingNode.Id, command.AssetId, out inverseCommands);
        }

        working.Assets.Remove(command.AssetId);
        affectedAssetIds.Add(command.AssetId);
        inverseCommands = new DesignCommand[] { new ImportAssetCommand(existingAsset) };
        return true;
    }

    private static bool TryDuplicateNode(
        MutableDocumentState working,
        DuplicateNodeCommand command,
        int commandIndex,
        ICollection<CommandFailure> errors,
        OrderedIdSet<NodeId> affectedNodeIds,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        if (!working.Nodes.TryGetValue(command.SourceNodeId, out var sourceNode))
        {
            return FailNode(errors, "node_not_found", $"Node '{command.SourceNodeId}' does not exist.", commandIndex, command.SourceNodeId, out inverseCommands);
        }

        if (sourceNode.Kind != command.Duplicate.Kind)
        {
            return FailNode(errors, "node_kind_mismatch", $"Duplicate '{command.Duplicate.Id}' must match source node kind '{sourceNode.Kind}'.", commandIndex, command.Duplicate.Id, out inverseCommands);
        }

        if (sourceNode is GroupNode && command.Duplicate is GroupNode duplicateGroup && duplicateGroup.ChildNodeIds.Count > 0)
        {
            return FailNode(errors, "duplicate_group_children_unsupported", $"Group duplication with child IDs is not supported in this phase.", commandIndex, command.Duplicate.Id, out inverseCommands);
        }

        return TryCreateNode(
            working,
            new CreateNodeCommand(command.Duplicate, command.InsertIndex),
            commandIndex,
            errors,
            affectedNodeIds,
            out inverseCommands);
    }

    private static bool TrySetVisibility(
        MutableDocumentState working,
        SetVisibilityCommand command,
        int commandIndex,
        ICollection<CommandFailure> errors,
        OrderedIdSet<NodeId> affectedNodeIds,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        if (!working.Nodes.TryGetValue(command.NodeId, out var existingNode))
        {
            return FailNode(errors, "node_not_found", $"Node '{command.NodeId}' does not exist.", commandIndex, command.NodeId, out inverseCommands);
        }

        working.Nodes[command.NodeId] = existingNode with { IsVisible = command.IsVisible };
        affectedNodeIds.Add(command.NodeId);
        inverseCommands = new DesignCommand[] { new SetVisibilityCommand(command.NodeId, existingNode.IsVisible) };
        return true;
    }

    private static bool TrySetLockState(
        MutableDocumentState working,
        SetLockStateCommand command,
        int commandIndex,
        ICollection<CommandFailure> errors,
        OrderedIdSet<NodeId> affectedNodeIds,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        if (!working.Nodes.TryGetValue(command.NodeId, out var existingNode))
        {
            return FailNode(errors, "node_not_found", $"Node '{command.NodeId}' does not exist.", commandIndex, command.NodeId, out inverseCommands);
        }

        working.Nodes[command.NodeId] = existingNode with { IsLocked = command.IsLocked };
        affectedNodeIds.Add(command.NodeId);
        inverseCommands = new DesignCommand[] { new SetLockStateCommand(command.NodeId, existingNode.IsLocked) };
        return true;
    }

    private static bool ValidateParent(
        MutableDocumentState working,
        NodeId? parentId,
        int commandIndex,
        ICollection<CommandFailure> errors,
        NodeId nodeId,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        if (parentId is null)
        {
            inverseCommands = Array.Empty<DesignCommand>();
            return true;
        }

        if (!working.Nodes.TryGetValue(parentId.Value, out var parentNode))
        {
            return FailNode(errors, "parent_not_found", $"Parent '{parentId.Value}' does not exist.", commandIndex, nodeId, out inverseCommands);
        }

        if (parentNode is not GroupNode)
        {
            return FailNode(errors, "parent_not_group", $"Parent '{parentId.Value}' must be a group.", commandIndex, nodeId, out inverseCommands);
        }

        inverseCommands = Array.Empty<DesignCommand>();
        return true;
    }

    private static bool ValidateInsertIndex(
        MutableDocumentState working,
        NodeId? parentId,
        int insertIndex,
        int commandIndex,
        ICollection<CommandFailure> errors,
        NodeId nodeId,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        var siblingCount = working.GetSiblingIds(parentId).Count;
        if (insertIndex < 0 || insertIndex > siblingCount)
        {
            return FailNode(errors, "insert_index_out_of_range", $"Insert index '{insertIndex}' is out of range for node '{nodeId}'.", commandIndex, nodeId, out inverseCommands);
        }

        inverseCommands = Array.Empty<DesignCommand>();
        return true;
    }

    private static bool FailCommand(
        ICollection<CommandFailure> errors,
        string code,
        string message,
        int commandIndex,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        errors.Add(new CommandFailure(code, message, commandIndex));
        inverseCommands = Array.Empty<DesignCommand>();
        return false;
    }

    private static bool FailNode(
        ICollection<CommandFailure> errors,
        string code,
        string message,
        int commandIndex,
        NodeId nodeId,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        errors.Add(new CommandFailure(code, message, commandIndex, nodeId));
        inverseCommands = Array.Empty<DesignCommand>();
        return false;
    }

    private static bool FailAsset(
        ICollection<CommandFailure> errors,
        string code,
        string message,
        int commandIndex,
        NodeId? nodeId,
        AssetId assetId,
        out IReadOnlyList<DesignCommand> inverseCommands)
    {
        errors.Add(new CommandFailure(code, message, commandIndex, nodeId, assetId));
        inverseCommands = Array.Empty<DesignCommand>();
        return false;
    }
}
