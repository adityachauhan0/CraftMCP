using System.Text.Json;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.Serialization;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Tests.Unit.Models;

public sealed class DocumentStateTests
{
    [Fact]
    public void EmptyDocument_RoundTripsWithSchemaVersion()
    {
        var document = new DocumentState(
            DocumentId.From("doc_homepage"),
            SchemaVersion.V1,
            "Homepage",
            new CanvasModel(1440, 900, CanvasPreset.DesktopFrame, new ColorValue(255, 255, 255)),
            new Dictionary<NodeId, NodeBase>(),
            Array.Empty<NodeId>(),
            new Dictionary<AssetId, AssetManifestEntry>());

        var json = JsonSerializer.Serialize(document, CraftJsonSerializerOptions.Default);
        var restored = JsonSerializer.Deserialize<DocumentState>(json, CraftJsonSerializerOptions.Default);

        Assert.NotNull(restored);
        Assert.Equal(document.Id, restored.Id);
        Assert.Equal(document.SchemaVersion, restored.SchemaVersion);
        Assert.Equal(document.Name, restored.Name);
        Assert.Equal(document.Canvas, restored.Canvas);
        Assert.Empty(restored.Nodes);
        Assert.Empty(restored.RootNodeIds);
        Assert.Empty(restored.Assets);
    }

    [Fact]
    public void DocumentState_CanRepresentGroupedScene()
    {
        var groupId = NodeId.From("node_group");
        var textId = NodeId.From("node_text");
        var group = new GroupNode(groupId, "Hero", TransformValue.Identity, null, true, false, OpacityValue.Full, new[] { textId });
        var text = new TextNode(
            textId,
            "Heading",
            TransformValue.Identity,
            groupId,
            true,
            false,
            OpacityValue.Full,
            "CraftMCP",
            new RectValue(10, 20, 200, 48),
            new TypographyStyle("Inter", 32, 700, "start", 1.1, 0),
            new ColorValue(0, 0, 0));

        var document = new DocumentState(
            DocumentId.From("doc_grouped"),
            SchemaVersion.V1,
            "Grouped",
            new CanvasModel(1200, 800, CanvasPreset.Custom, new ColorValue(255, 255, 255)),
            new Dictionary<NodeId, NodeBase>
            {
                [groupId] = group,
                [textId] = text,
            },
            new[] { groupId },
            new Dictionary<AssetId, AssetManifestEntry>());

        Assert.Single(document.RootNodeIds);
        Assert.Equal(2, document.Nodes.Count);
    }
}
