using System.Text.Json;
using System.Text.Json.Serialization;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.Serialization;
using CraftMCP.Domain.Validation;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Domain.Exports;

public static class DocumentJsonExporter
{
    public static string Serialize(DocumentState document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var validation = DocumentValidator.Validate(document);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(error => error.Message));
            throw new InvalidOperationException($"DocumentState must be valid before export. {errors}");
        }

        var export = new ExportEnvelope(
            document.SchemaVersion,
            new ExportDocument(
                document.Id,
                document.Name,
                ExportCanvas.From(document.Canvas),
                document.RootNodeIds,
                EnumerateSceneNodes(document).Select(MapNode).ToArray(),
                document.Assets.Values
                    .OrderBy(asset => asset.Id.Value, StringComparer.Ordinal)
                    .Select(asset => new ExportAsset(asset.Id, asset.FileName, asset.MediaType, asset.ContentHash))
                    .ToArray()));

        return JsonSerializer.Serialize(export, CraftJsonSerializerOptions.Export).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    private static IReadOnlyList<NodeBase> EnumerateSceneNodes(DocumentState document)
    {
        var orderedNodes = new List<NodeBase>(document.Nodes.Count);

        foreach (var rootId in document.RootNodeIds)
        {
            AppendNode(rootId, document, orderedNodes);
        }

        return orderedNodes;
    }

    private static void AppendNode(NodeId nodeId, DocumentState document, ICollection<NodeBase> orderedNodes)
    {
        var node = document.Nodes[nodeId];
        orderedNodes.Add(node);

        if (node is not GroupNode group)
        {
            return;
        }

        foreach (var childId in group.ChildNodeIds)
        {
            AppendNode(childId, document, orderedNodes);
        }
    }

    private static ExportNode MapNode(NodeBase node)
    {
        var exportNode = new ExportNode
        {
            Id = node.Id,
            Type = node switch
            {
                TextNode => "text",
                RectangleNode => "rectangle",
                CircleNode => "circle",
                LineNode => "line",
                ImageNode => "image",
                GroupNode => "group",
                _ => throw new NotSupportedException($"Unsupported node type '{node.GetType().Name}'."),
            },
            Name = node.Name,
            Transform = node.Transform,
            ParentId = node.ParentId,
            IsVisible = node.IsVisible,
            IsLocked = node.IsLocked,
            Opacity = node.Opacity,
        };

        switch (node)
        {
            case TextNode text:
                exportNode.Content = text.Content;
                exportNode.Bounds = text.Bounds;
                exportNode.Typography = text.Typography;
                exportNode.Fill = text.Fill;
                break;
            case RectangleNode rectangle:
                exportNode.Size = rectangle.Size;
                exportNode.Fill = rectangle.Fill;
                exportNode.Stroke = rectangle.Stroke;
                exportNode.CornerRadius = rectangle.CornerRadius;
                break;
            case CircleNode circle:
                exportNode.Size = circle.Size;
                exportNode.Fill = circle.Fill;
                exportNode.Stroke = circle.Stroke;
                break;
            case LineNode line:
                exportNode.Start = line.Start;
                exportNode.End = line.End;
                exportNode.Stroke = line.Stroke;
                break;
            case ImageNode image:
                exportNode.Bounds = image.Bounds;
                exportNode.Asset = image.Asset;
                exportNode.FitMode = image.FitMode;
                exportNode.Crop = image.Crop;
                break;
            case GroupNode group:
                exportNode.ChildNodeIds = group.ChildNodeIds;
                break;
        }

        return exportNode;
    }

    private sealed record ExportEnvelope(SchemaVersion SchemaVersion, ExportDocument Document);

    private sealed record ExportDocument(
        DocumentId Id,
        string Name,
        ExportCanvas Canvas,
        IReadOnlyList<NodeId> RootNodeIds,
        IReadOnlyList<ExportNode> Nodes,
        IReadOnlyList<ExportAsset> Assets);

    private sealed record ExportCanvas(
        double Width,
        double Height,
        CanvasPreset Preset,
        ColorValue Background,
        SafeAreaInsets? SafeArea)
    {
        public static ExportCanvas From(CanvasModel canvas) =>
            new(canvas.Width, canvas.Height, canvas.Preset, canvas.Background, canvas.SafeArea);
    }

    private sealed class ExportNode
    {
        public NodeId Id { get; init; }

        public string Type { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public TransformValue Transform { get; init; }

        public NodeId? ParentId { get; init; }

        public bool IsVisible { get; init; }

        public bool IsLocked { get; init; }

        public OpacityValue Opacity { get; init; }

        public string? Content { get; set; }

        public RectValue? Bounds { get; set; }

        public TypographyStyle? Typography { get; set; }

        public SizeValue? Size { get; set; }

        public PointValue? Start { get; set; }

        public PointValue? End { get; set; }

        public ColorValue? Fill { get; set; }

        public StrokeStyle? Stroke { get; set; }

        public double? CornerRadius { get; set; }

        public AssetReference? Asset { get; set; }

        public string? FitMode { get; set; }

        public RectValue? Crop { get; set; }

        public IReadOnlyList<NodeId>? ChildNodeIds { get; set; }
    }

    private sealed record ExportAsset(AssetId Id, string FileName, string MediaType, string ContentHash);
}
