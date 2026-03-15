using Avalonia;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.App.Services;

public sealed class NodeFactory
{
    private static readonly ColorValue DefaultFill = new(96, 165, 250);
    private static readonly ColorValue DefaultStroke = new(30, 41, 59);
    private static readonly ColorValue DefaultTextColor = new(15, 23, 42);

    public RectangleNode CreateRectangle(Rect rect, NodeId? parentId = null) =>
        new(
            StableIdGenerator.CreateNodeId(),
            "Rectangle",
            new TransformValue(rect.X, rect.Y, 1, 1, 0),
            parentId,
            true,
            false,
            OpacityValue.Full,
            new SizeValue(rect.Width, rect.Height),
            DefaultFill,
            new StrokeStyle(DefaultStroke, 2),
            12);

    public CircleNode CreateCircle(Rect rect, NodeId? parentId = null) =>
        new(
            StableIdGenerator.CreateNodeId(),
            "Circle",
            new TransformValue(rect.X, rect.Y, 1, 1, 0),
            parentId,
            true,
            false,
            OpacityValue.Full,
            new SizeValue(rect.Width, rect.Height),
            new ColorValue(250, 204, 21),
            new StrokeStyle(DefaultStroke, 2));

    public LineNode CreateLine(Point start, Point end, NodeId? parentId = null)
    {
        var originX = Math.Min(start.X, end.X);
        var originY = Math.Min(start.Y, end.Y);
        var localStart = new PointValue(start.X - originX, start.Y - originY);
        var localEnd = new PointValue(end.X - originX, end.Y - originY);

        return new LineNode(
            StableIdGenerator.CreateNodeId(),
            "Line",
            new TransformValue(originX, originY, 1, 1, 0),
            parentId,
            true,
            false,
            OpacityValue.Full,
            localStart,
            localEnd,
            new StrokeStyle(DefaultStroke, 4));
    }

    public TextNode CreateText(Point point, NodeId? parentId = null) =>
        new(
            StableIdGenerator.CreateNodeId(),
            "Text",
            new TransformValue(point.X, point.Y, 1, 1, 0),
            parentId,
            true,
            false,
            OpacityValue.Full,
            "New text",
            new RectValue(0, 0, 280, 96),
            new TypographyStyle("Inter", 32, 600, "start", 1.1, 0),
            DefaultTextColor);

    public ImageNode CreateImage(Rect rect, AssetId assetId, NodeId? parentId = null) =>
        new(
            StableIdGenerator.CreateNodeId(),
            "Image",
            new TransformValue(rect.X, rect.Y, 1, 1, 0),
            parentId,
            true,
            false,
            OpacityValue.Full,
            new RectValue(0, 0, rect.Width, rect.Height),
            new AssetReference(assetId),
            "cover",
            null);

    public NodeBase CreateDuplicate(NodeBase node, double deltaX = 32d, double deltaY = 32d) =>
        node switch
        {
            RectangleNode rectangle => rectangle with
            {
                Id = StableIdGenerator.CreateNodeId(),
                Name = $"{rectangle.Name} Copy",
                Transform = OffsetTransform(rectangle.Transform, deltaX, deltaY),
            },
            CircleNode circle => circle with
            {
                Id = StableIdGenerator.CreateNodeId(),
                Name = $"{circle.Name} Copy",
                Transform = OffsetTransform(circle.Transform, deltaX, deltaY),
            },
            LineNode line => line with
            {
                Id = StableIdGenerator.CreateNodeId(),
                Name = $"{line.Name} Copy",
                Transform = OffsetTransform(line.Transform, deltaX, deltaY),
            },
            TextNode text => text with
            {
                Id = StableIdGenerator.CreateNodeId(),
                Name = $"{text.Name} Copy",
                Transform = OffsetTransform(text.Transform, deltaX, deltaY),
            },
            ImageNode image => image with
            {
                Id = StableIdGenerator.CreateNodeId(),
                Name = $"{image.Name} Copy",
                Transform = OffsetTransform(image.Transform, deltaX, deltaY),
            },
            GroupNode => throw new NotSupportedException("Group duplication is deferred until grouped subtree duplication is supported."),
            _ => throw new NotSupportedException($"Unsupported node type '{node.GetType().Name}'."),
        };

    public NodeBase MoveNode(NodeBase node, double deltaX, double deltaY) =>
        node with
        {
            Transform = OffsetTransform(node.Transform, deltaX, deltaY),
        };

    public NodeBase RotateNode(NodeBase node, double rotationDegrees) =>
        node with
        {
            Transform = node.Transform with { RotationDegrees = rotationDegrees },
        };

    public NodeBase ResizeNodeToBounds(NodeBase node, Rect bounds) =>
        node switch
        {
            RectangleNode rectangle => rectangle with
            {
                Transform = rectangle.Transform with { X = bounds.X, Y = bounds.Y },
                Size = new SizeValue(bounds.Width, bounds.Height),
            },
            CircleNode circle => circle with
            {
                Transform = circle.Transform with { X = bounds.X, Y = bounds.Y },
                Size = new SizeValue(bounds.Width, bounds.Height),
            },
            TextNode text => text with
            {
                Transform = text.Transform with { X = bounds.X, Y = bounds.Y },
                Bounds = new RectValue(0, 0, bounds.Width, bounds.Height),
            },
            ImageNode image => image with
            {
                Transform = image.Transform with { X = bounds.X, Y = bounds.Y },
                Bounds = new RectValue(0, 0, bounds.Width, bounds.Height),
            },
            _ => node,
        };

    private static TransformValue OffsetTransform(TransformValue transform, double deltaX, double deltaY) =>
        transform with
        {
            X = transform.X + deltaX,
            Y = transform.Y + deltaY,
        };
}
