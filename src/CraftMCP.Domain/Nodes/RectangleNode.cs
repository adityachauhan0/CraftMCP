using System.Text.Json.Serialization;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Domain.Nodes;

public sealed record RectangleNode(
    NodeId Id,
    string Name,
    TransformValue Transform,
    NodeId? ParentId,
    bool IsVisible,
    bool IsLocked,
    OpacityValue Opacity,
    SizeValue Size,
    ColorValue Fill,
    StrokeStyle? Stroke,
    double CornerRadius) : NodeBase(Id, Name, Transform, ParentId, IsVisible, IsLocked, Opacity)
{
    [JsonIgnore]
    public override NodeKind Kind => NodeKind.Rectangle;
}
