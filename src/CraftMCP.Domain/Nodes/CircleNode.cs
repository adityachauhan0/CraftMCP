using System.Text.Json.Serialization;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Domain.Nodes;

public sealed record CircleNode(
    NodeId Id,
    string Name,
    TransformValue Transform,
    NodeId? ParentId,
    bool IsVisible,
    bool IsLocked,
    OpacityValue Opacity,
    SizeValue Size,
    ColorValue Fill,
    StrokeStyle? Stroke) : NodeBase(Id, Name, Transform, ParentId, IsVisible, IsLocked, Opacity)
{
    [JsonIgnore]
    public override NodeKind Kind => NodeKind.Circle;
}
