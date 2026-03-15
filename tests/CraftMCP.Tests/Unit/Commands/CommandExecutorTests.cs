using CraftMCP.Domain.Commands;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.ValueObjects;
using CraftMCP.Tests.TestSupport;

namespace CraftMCP.Tests.Unit.Commands;

public sealed class CommandExecutorTests
{
    [Fact]
    public void Execute_FailsAtomicallyWhenAnyCommandInTheBatchIsInvalid()
    {
        var document = DocumentExportFixtureFactory.CreateSlide();
        var createdNodeId = NodeId.From("node_slide_callout");
        var batch = CreateHumanBatch(
            "Create a callout and then touch a missing node",
            new CreateNodeCommand(CreateRectangleNode(createdNodeId), 1),
            new SetVisibilityCommand(NodeId.From("node_missing"), false));

        var outcome = CommandExecutor.Execute(document, batch);

        Assert.False(outcome.Result.IsSuccess);
        Assert.Same(document, outcome.Document);
        Assert.Null(outcome.HistoryEntry);
        Assert.DoesNotContain(createdNodeId, outcome.Document.RootNodeIds);
        Assert.Contains(outcome.Result.Errors, error => error.Code == "node_not_found");
    }

    [Fact]
    public void Execute_AppliesCreateUpdateDuplicateAndDeleteCommands()
    {
        var document = DocumentExportFixtureFactory.CreateSlide();
        var createdNodeId = NodeId.From("node_slide_callout");
        var duplicateNodeId = NodeId.From("node_slide_callout_copy");
        var batch = CreateHumanBatch(
            "Create, update, duplicate, and delete a callout",
            new CreateNodeCommand(CreateRectangleNode(createdNodeId), 1),
            new UpdateNodeCommand(CreateRectangleNode(createdNodeId) with
            {
                Name = "Updated Callout",
                CornerRadius = 24,
            }),
            new DuplicateNodeCommand(
                createdNodeId,
                CreateRectangleNode(duplicateNodeId) with
                {
                    Name = "Updated Callout Copy",
                    Transform = new TransformValue(420, 220, 1, 1, 0),
                },
                2),
            new DeleteNodeCommand(duplicateNodeId));

        var outcome = CommandExecutor.Execute(document, batch);

        Assert.True(outcome.Result.IsSuccess);
        Assert.NotNull(outcome.HistoryEntry);
        Assert.Contains(createdNodeId, outcome.Document.RootNodeIds);
        Assert.DoesNotContain(duplicateNodeId, outcome.Document.RootNodeIds);

        var createdNode = Assert.IsType<RectangleNode>(outcome.Document.Nodes[createdNodeId]);
        Assert.Equal("Updated Callout", createdNode.Name);
        Assert.Equal(24, createdNode.CornerRadius);
    }

    [Fact]
    public void Execute_AppliesGroupReorderAndUngroupCommands()
    {
        var document = DocumentExportFixtureFactory.CreateUiMockup();
        var statusId = NodeId.From("node_ui_status");
        var titleId = NodeId.From("node_ui_title");
        var dividerId = NodeId.From("node_ui_divider");
        var groupId = NodeId.From("node_ui_header_group");

        var grouped = CommandExecutor.Execute(
            document,
            CreateAgentBatch(
                "Group header content",
                new GroupNodesCommand(
                    new GroupNode(
                        groupId,
                        "Header Group",
                        TransformValue.Identity,
                        null,
                        true,
                        false,
                        OpacityValue.Full,
                        new[] { statusId, titleId }),
                    1)));

        Assert.True(grouped.Result.IsSuccess);
        Assert.Contains(groupId, grouped.Document.RootNodeIds);

        var reordered = CommandExecutor.Execute(
            grouped.Document,
            CreateHumanBatch(
                "Move the divider into the group",
                new ReorderNodeCommand(dividerId, groupId, 2)));

        Assert.True(reordered.Result.IsSuccess);
        var reorderedGroup = Assert.IsType<GroupNode>(reordered.Document.Nodes[groupId]);
        Assert.Equal(new[] { statusId, titleId, dividerId }, reorderedGroup.ChildNodeIds);
        Assert.Equal(groupId, reordered.Document.Nodes[dividerId].ParentId);

        var ungrouped = CommandExecutor.Execute(
            reordered.Document,
            CreateHumanBatch(
                "Ungroup the header",
                new UngroupNodeCommand(groupId)));

        Assert.True(ungrouped.Result.IsSuccess);
        Assert.DoesNotContain(groupId, ungrouped.Document.Nodes.Keys);
        Assert.Equal(
            new[]
            {
                NodeId.From("node_ui_phone_frame"),
                statusId,
                titleId,
                dividerId,
                NodeId.From("node_ui_card_group"),
            },
            ungrouped.Document.RootNodeIds);
        Assert.Null(ungrouped.Document.Nodes[statusId].ParentId);
        Assert.Null(ungrouped.Document.Nodes[titleId].ParentId);
        Assert.Null(ungrouped.Document.Nodes[dividerId].ParentId);
    }

    [Fact]
    public void Execute_AppliesCanvasAssetAndFlagCommands()
    {
        var document = DocumentExportFixtureFactory.CreateSlide();
        var titleId = NodeId.From("node_slide_title");
        var panelId = NodeId.From("node_slide_panel");
        var assetId = AssetId.From("asset_slide_texture");
        var batch = CreateHumanBatch(
            "Refresh the canvas and scene flags",
            new SetCanvasCommand(new CanvasModel(1920, 1080, CanvasPreset.Custom, new ColorValue(12, 18, 29))),
            new ImportAssetCommand(new AssetManifestEntry(
                assetId,
                "texture.png",
                "image/png",
                "hash-slide-texture")),
            new SetVisibilityCommand(titleId, false),
            new SetLockStateCommand(panelId, true));

        var outcome = CommandExecutor.Execute(document, batch);

        Assert.True(outcome.Result.IsSuccess);
        Assert.Equal(1920, outcome.Document.Canvas.Width);
        Assert.Equal(1080, outcome.Document.Canvas.Height);
        Assert.Contains(assetId, outcome.Document.Assets.Keys);
        Assert.False(outcome.Document.Nodes[titleId].IsVisible);
        Assert.True(outcome.Document.Nodes[panelId].IsLocked);
    }

    private static CommandBatch CreateHumanBatch(string summary, params DesignCommand[] commands) =>
        new(
            summary,
            commands,
            new CommandProvenance(CommandSource.Human, "user:test"),
            null);

    private static CommandBatch CreateAgentBatch(string summary, params DesignCommand[] commands) =>
        new(
            summary,
            commands,
            new CommandProvenance(CommandSource.Agent, "planner:test", "prompt:test"),
            "Generated from prompt context.");

    private static RectangleNode CreateRectangleNode(NodeId id) =>
        new(
            id,
            "Callout",
            new TransformValue(120, 220, 1, 1, 0),
            null,
            true,
            false,
            OpacityValue.Full,
            new SizeValue(320, 180),
            new ColorValue(255, 255, 255),
            new StrokeStyle(new ColorValue(220, 226, 232), 1),
            12);
}
