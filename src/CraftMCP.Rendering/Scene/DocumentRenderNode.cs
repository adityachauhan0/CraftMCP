using System.Numerics;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Nodes;

namespace CraftMCP.Rendering.Scene;

public sealed record DocumentRenderNode(
    NodeBase Node,
    Matrix3x2 WorldTransform,
    double EffectiveOpacity,
    AxisAlignedBounds Bounds,
    bool IsDrawable)
{
    public NodeId NodeId => Node.Id;
}
