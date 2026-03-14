using System.Text.Json.Serialization;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Domain.Nodes;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextNode), "text")]
[JsonDerivedType(typeof(RectangleNode), "rectangle")]
[JsonDerivedType(typeof(CircleNode), "circle")]
[JsonDerivedType(typeof(LineNode), "line")]
[JsonDerivedType(typeof(ImageNode), "image")]
[JsonDerivedType(typeof(GroupNode), "group")]
public abstract record NodeBase(
    NodeId Id,
    string Name,
    TransformValue Transform,
    NodeId? ParentId,
    bool IsVisible,
    bool IsLocked,
    OpacityValue Opacity)
{
    [JsonIgnore]
    public abstract NodeKind Kind { get; }
}
