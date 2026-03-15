using System.Numerics;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Nodes;
using SkiaSharp;

namespace CraftMCP.Rendering.Scene;

public sealed class DocumentRenderPlanBuilder
{
    public DocumentRenderPlan Build(DocumentState document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var nodes = new List<DocumentRenderNode>();

        foreach (var rootNodeId in document.RootNodeIds)
        {
            TraverseNode(document, rootNodeId, Matrix3x2.Identity, 1d, nodes);
        }

        return new DocumentRenderPlan(
            document.Id,
            document.Canvas.Width,
            document.Canvas.Height,
            document.Canvas.Background,
            document.Canvas.SafeArea,
            nodes);
    }

    private static AxisAlignedBounds? TraverseNode(
        DocumentState document,
        CraftMCP.Domain.Ids.NodeId nodeId,
        Matrix3x2 parentTransform,
        double parentOpacity,
        List<DocumentRenderNode> nodes)
    {
        var node = document.Nodes[nodeId];
        if (!node.IsVisible)
        {
            return null;
        }

        var worldTransform = CreateLocalTransform(node.Transform) * parentTransform;
        var effectiveOpacity = parentOpacity * node.Opacity.Value;

        if (node is GroupNode group)
        {
            var groupIndex = nodes.Count;
            var placeholderBounds = new AxisAlignedBounds(
                node.Transform.X,
                node.Transform.Y,
                node.Transform.X,
                node.Transform.Y);

            nodes.Add(new DocumentRenderNode(node, worldTransform, effectiveOpacity, placeholderBounds, false));

            AxisAlignedBounds? groupBounds = null;
            foreach (var childNodeId in group.ChildNodeIds)
            {
                var childBounds = TraverseNode(document, childNodeId, worldTransform, effectiveOpacity, nodes);
                if (childBounds is null)
                {
                    continue;
                }

                groupBounds = groupBounds is null
                    ? childBounds
                    : AxisAlignedBounds.Union(groupBounds.Value, childBounds.Value);
            }

            if (groupBounds is not null)
            {
                nodes[groupIndex] = nodes[groupIndex] with { Bounds = groupBounds.Value };
            }

            return groupBounds;
        }

        var localBounds = GetLocalBounds(node);
        var bounds = TransformBounds(localBounds, worldTransform);
        nodes.Add(new DocumentRenderNode(node, worldTransform, effectiveOpacity, bounds, true));
        return bounds;
    }

    private static Matrix3x2 CreateLocalTransform(CraftMCP.Domain.ValueObjects.TransformValue transform) =>
        Matrix3x2.CreateScale((float)transform.ScaleX, (float)transform.ScaleY) *
        Matrix3x2.CreateRotation((float)(Math.PI * transform.RotationDegrees / 180d)) *
        Matrix3x2.CreateTranslation((float)transform.X, (float)transform.Y);

    private static SKRect GetLocalBounds(NodeBase node) =>
        node switch
        {
            RectangleNode rectangle => SKRect.Create(0, 0, (float)rectangle.Size.Width, (float)rectangle.Size.Height),
            CircleNode circle => SKRect.Create(0, 0, (float)circle.Size.Width, (float)circle.Size.Height),
            LineNode line => GetLineBounds(line),
            TextNode text => SKRect.Create((float)text.Bounds.X, (float)text.Bounds.Y, (float)text.Bounds.Width, (float)text.Bounds.Height),
            ImageNode image => SKRect.Create((float)image.Bounds.X, (float)image.Bounds.Y, (float)image.Bounds.Width, (float)image.Bounds.Height),
            _ => throw new InvalidOperationException($"Unsupported node type '{node.GetType().Name}'."),
        };

    private static SKRect GetLineBounds(LineNode line)
    {
        var halfStroke = (float)line.Stroke.Width / 2f;
        var left = Math.Min(line.Start.X, line.End.X) - halfStroke;
        var top = Math.Min(line.Start.Y, line.End.Y) - halfStroke;
        var right = Math.Max(line.Start.X, line.End.X) + halfStroke;
        var bottom = Math.Max(line.Start.Y, line.End.Y) + halfStroke;
        return new SKRect((float)left, (float)top, (float)right, (float)bottom);
    }

    private static AxisAlignedBounds TransformBounds(SKRect rect, Matrix3x2 transform)
    {
        var topLeft = Vector2.Transform(new Vector2(rect.Left, rect.Top), transform);
        var topRight = Vector2.Transform(new Vector2(rect.Right, rect.Top), transform);
        var bottomLeft = Vector2.Transform(new Vector2(rect.Left, rect.Bottom), transform);
        var bottomRight = Vector2.Transform(new Vector2(rect.Right, rect.Bottom), transform);

        var left = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
        var top = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
        var right = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
        var bottom = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));
        return new AxisAlignedBounds(left, top, right, bottom);
    }
}
