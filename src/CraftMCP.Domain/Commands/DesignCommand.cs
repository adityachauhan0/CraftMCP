using System.Text.Json.Serialization;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Nodes;

namespace CraftMCP.Domain.Commands;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CreateNodeCommand), "createNode")]
[JsonDerivedType(typeof(UpdateNodeCommand), "updateNode")]
[JsonDerivedType(typeof(DeleteNodeCommand), "deleteNode")]
[JsonDerivedType(typeof(ReorderNodeCommand), "reorderNode")]
[JsonDerivedType(typeof(GroupNodesCommand), "groupNodes")]
[JsonDerivedType(typeof(UngroupNodeCommand), "ungroupNode")]
[JsonDerivedType(typeof(SetCanvasCommand), "setCanvas")]
[JsonDerivedType(typeof(ImportAssetCommand), "importAsset")]
[JsonDerivedType(typeof(DuplicateNodeCommand), "duplicateNode")]
[JsonDerivedType(typeof(SetVisibilityCommand), "setVisibility")]
[JsonDerivedType(typeof(SetLockStateCommand), "setLockState")]
public abstract record DesignCommand
{
    [JsonIgnore]
    public abstract CommandKind Kind { get; }
}

public sealed record CreateNodeCommand(NodeBase Node, int InsertIndex) : DesignCommand
{
    public override CommandKind Kind => CommandKind.CreateNode;
}

public sealed record UpdateNodeCommand(NodeBase Node) : DesignCommand
{
    public override CommandKind Kind => CommandKind.UpdateNode;
}

public sealed record DeleteNodeCommand(NodeId NodeId) : DesignCommand
{
    public override CommandKind Kind => CommandKind.DeleteNode;
}

public sealed record ReorderNodeCommand(NodeId NodeId, NodeId? NewParentId, int InsertIndex) : DesignCommand
{
    public override CommandKind Kind => CommandKind.ReorderNode;
}

public sealed record GroupNodesCommand(GroupNode Group, int InsertIndex) : DesignCommand
{
    public override CommandKind Kind => CommandKind.GroupNodes;
}

public sealed record UngroupNodeCommand(NodeId GroupId) : DesignCommand
{
    public override CommandKind Kind => CommandKind.UngroupNode;
}

public sealed record SetCanvasCommand(CanvasModel Canvas) : DesignCommand
{
    public override CommandKind Kind => CommandKind.SetCanvas;
}

public sealed record ImportAssetCommand(AssetManifestEntry Asset) : DesignCommand
{
    public override CommandKind Kind => CommandKind.ImportAsset;
}

public sealed record DuplicateNodeCommand(NodeId SourceNodeId, NodeBase Duplicate, int InsertIndex) : DesignCommand
{
    public override CommandKind Kind => CommandKind.DuplicateNode;
}

public sealed record SetVisibilityCommand(NodeId NodeId, bool IsVisible) : DesignCommand
{
    public override CommandKind Kind => CommandKind.SetVisibility;
}

public sealed record SetLockStateCommand(NodeId NodeId, bool IsLocked) : DesignCommand
{
    public override CommandKind Kind => CommandKind.SetLockState;
}

public enum CommandKind
{
    CreateNode,
    UpdateNode,
    DeleteNode,
    ReorderNode,
    GroupNodes,
    UngroupNode,
    SetCanvas,
    ImportAsset,
    DuplicateNode,
    SetVisibility,
    SetLockState,
}
