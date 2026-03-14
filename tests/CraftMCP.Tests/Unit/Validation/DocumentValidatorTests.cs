using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.Validation;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Tests.Unit.Validation;

public sealed class DocumentValidatorTests
{
    [Fact]
    public void Validate_ReturnsNoErrorsForValidHierarchy()
    {
        var result = DocumentValidator.Validate(CreateValidDocument());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_RejectsMissingRootReference()
    {
        var document = CreateValidDocument() with
        {
            RootNodeIds = new[] { NodeId.From("node_missing") }
        };

        var result = DocumentValidator.Validate(document);

        Assert.Contains(result.Errors, error => error.Code == "root_missing");
    }

    [Fact]
    public void Validate_RejectsMissingParentReference()
    {
        var missingParent = NodeId.From("node_missing_parent");
        var textId = NodeId.From("node_text");
        var document = CreateValidDocument() with
        {
            Nodes = new Dictionary<NodeId, NodeBase>
            {
                [textId] = new TextNode(
                    textId,
                    "Loose",
                    TransformValue.Identity,
                    missingParent,
                    true,
                    false,
                    OpacityValue.Full,
                    "Loose",
                    new RectValue(0, 0, 100, 20),
                    new TypographyStyle("Inter", 14, 400, "start", 1, 0),
                    new ColorValue(0, 0, 0)),
            },
            RootNodeIds = Array.Empty<NodeId>()
        };

        var result = DocumentValidator.Validate(document);

        Assert.Contains(result.Errors, error => error.Code == "parent_missing");
    }

    [Fact]
    public void Validate_RejectsCycles()
    {
        var firstId = NodeId.From("node_first");
        var secondId = NodeId.From("node_second");
        var document = new DocumentState(
            DocumentId.From("doc_cycle"),
            SchemaVersion.V1,
            "Cycle",
            new CanvasModel(800, 600, CanvasPreset.Custom, new ColorValue(255, 255, 255)),
            new Dictionary<NodeId, NodeBase>
            {
                [firstId] = new GroupNode(firstId, "First", TransformValue.Identity, secondId, true, false, OpacityValue.Full, new[] { secondId }),
                [secondId] = new GroupNode(secondId, "Second", TransformValue.Identity, firstId, true, false, OpacityValue.Full, new[] { firstId }),
            },
            Array.Empty<NodeId>(),
            new Dictionary<AssetId, AssetManifestEntry>());

        var result = DocumentValidator.Validate(document);

        Assert.Contains(result.Errors, error => error.Code == "cycle_detected");
    }

    [Fact]
    public void Validate_RejectsDuplicatePlacement()
    {
        var groupId = NodeId.From("node_group");
        var textId = NodeId.From("node_text");
        var group = new GroupNode(groupId, "Group", TransformValue.Identity, null, true, false, OpacityValue.Full, new[] { textId, textId });
        var text = new TextNode(
            textId,
            "Title",
            TransformValue.Identity,
            groupId,
            true,
            false,
            OpacityValue.Full,
            "Title",
            new RectValue(0, 0, 100, 20),
            new TypographyStyle("Inter", 16, 400, "start", 1, 0),
            new ColorValue(0, 0, 0));

        var document = new DocumentState(
            DocumentId.From("doc_duplicate"),
            SchemaVersion.V1,
            "Duplicate",
            new CanvasModel(800, 600, CanvasPreset.Custom, new ColorValue(255, 255, 255)),
            new Dictionary<NodeId, NodeBase>
            {
                [groupId] = group,
                [textId] = text,
            },
            new[] { groupId },
            new Dictionary<AssetId, AssetManifestEntry>());

        var result = DocumentValidator.Validate(document);

        Assert.Contains(result.Errors, error => error.Code == "node_referenced_multiple_times");
    }

    private static DocumentState CreateValidDocument()
    {
        var groupId = NodeId.From("node_group");
        var textId = NodeId.From("node_text");
        var imageId = NodeId.From("node_image");
        var assetId = AssetId.From("asset_image");

        return new DocumentState(
            DocumentId.From("doc_valid"),
            SchemaVersion.V1,
            "Valid",
            new CanvasModel(1080, 1080, CanvasPreset.SquarePost, new ColorValue(255, 255, 255)),
            new Dictionary<NodeId, NodeBase>
            {
                [groupId] = new GroupNode(groupId, "Group", TransformValue.Identity, null, true, false, OpacityValue.Full, new[] { textId }),
                [textId] = new TextNode(
                    textId,
                    "Heading",
                    TransformValue.Identity,
                    groupId,
                    true,
                    false,
                    OpacityValue.Full,
                    "Heading",
                    new RectValue(80, 80, 300, 64),
                    new TypographyStyle("Inter", 32, 700, "start", 1.1, 0),
                    new ColorValue(0, 0, 0)),
                [imageId] = new ImageNode(
                    imageId,
                    "Hero Image",
                    new TransformValue(400, 120, 1, 1, 0),
                    null,
                    true,
                    false,
                    OpacityValue.Full,
                    new RectValue(0, 0, 320, 240),
                    new AssetReference(assetId),
                    "cover",
                    null),
            },
            new[] { groupId, imageId },
            new Dictionary<AssetId, AssetManifestEntry>
            {
                [assetId] = new AssetManifestEntry(assetId, "hero.png", "image/png", "hash-hero"),
            });
    }
}
