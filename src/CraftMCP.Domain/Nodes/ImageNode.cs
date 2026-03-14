using System.Text.Json.Serialization;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Domain.Nodes;

public sealed record ImageNode(
    NodeId Id,
    string Name,
    TransformValue Transform,
    NodeId? ParentId,
    bool IsVisible,
    bool IsLocked,
    OpacityValue Opacity,
    RectValue Bounds,
    AssetReference Asset,
    string FitMode,
    RectValue? Crop) : NodeBase(Id, Name, Transform, ParentId, IsVisible, IsLocked, Opacity)
{
    [JsonIgnore]
    public override NodeKind Kind => NodeKind.Image;
}
