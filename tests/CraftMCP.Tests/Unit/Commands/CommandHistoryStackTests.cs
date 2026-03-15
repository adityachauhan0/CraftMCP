using CraftMCP.Domain.Commands;
using CraftMCP.Domain.Exports;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.ValueObjects;
using CraftMCP.Tests.TestSupport;

namespace CraftMCP.Tests.Unit.Commands;

public sealed class CommandHistoryStackTests
{
    [Fact]
    public void HistoryStack_UndoesAndRedoesMixedHumanAndAgentBatches()
    {
        var initialDocument = DocumentExportFixtureFactory.CreateUiMockup();
        var statusId = NodeId.From("node_ui_status");
        var titleId = NodeId.From("node_ui_title");
        var dividerId = NodeId.From("node_ui_divider");
        var buttonId = NodeId.From("node_ui_button");
        var groupId = NodeId.From("node_ui_header_group");
        var assetId = AssetId.From("asset_ui_texture");

        var visibilityBatch = CreateHumanBatch(
            "Hide the divider",
            new SetVisibilityCommand(dividerId, false));
        var groupBatch = CreateAgentBatch(
            "Group the header text",
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
                1));
        var importAndLockBatch = CreateHumanBatch(
            "Import a texture and lock the button",
            new ImportAssetCommand(new AssetManifestEntry(
                assetId,
                "texture.png",
                "image/png",
                "hash-ui-texture")),
            new SetLockStateCommand(buttonId, true));

        var history = CommandHistoryStack.Empty;

        var firstCommit = history.Commit(initialDocument, visibilityBatch);
        Assert.True(firstCommit.Result.IsSuccess);
        Assert.Single(firstCommit.History.UndoEntries);

        var secondCommit = firstCommit.History.Commit(firstCommit.Document, groupBatch);
        Assert.True(secondCommit.Result.IsSuccess);
        Assert.Equal(2, secondCommit.History.UndoEntries.Count);

        var thirdCommit = secondCommit.History.Commit(secondCommit.Document, importAndLockBatch);
        Assert.True(thirdCommit.Result.IsSuccess);
        Assert.Equal(3, thirdCommit.History.UndoEntries.Count);
        Assert.Empty(thirdCommit.History.RedoEntries);

        var finalDocumentJson = DocumentJsonExporter.Serialize(thirdCommit.Document);

        var undoOne = thirdCommit.History.Undo(thirdCommit.Document);
        Assert.True(undoOne.Result.IsSuccess);
        Assert.Equal(2, undoOne.History.UndoEntries.Count);
        Assert.Single(undoOne.History.RedoEntries);

        var undoTwo = undoOne.History.Undo(undoOne.Document);
        Assert.True(undoTwo.Result.IsSuccess);
        Assert.Single(undoTwo.History.UndoEntries);
        Assert.Equal(2, undoTwo.History.RedoEntries.Count);

        var undoThree = undoTwo.History.Undo(undoTwo.Document);
        Assert.True(undoThree.Result.IsSuccess);
        Assert.Empty(undoThree.History.UndoEntries);
        Assert.Equal(3, undoThree.History.RedoEntries.Count);
        Assert.Equal(DocumentJsonExporter.Serialize(initialDocument), DocumentJsonExporter.Serialize(undoThree.Document));

        var redoOne = undoThree.History.Redo(undoThree.Document);
        Assert.True(redoOne.Result.IsSuccess);
        Assert.Single(redoOne.History.UndoEntries);
        Assert.Equal(2, redoOne.History.RedoEntries.Count);

        var redoTwo = redoOne.History.Redo(redoOne.Document);
        Assert.True(redoTwo.Result.IsSuccess);
        Assert.Equal(2, redoTwo.History.UndoEntries.Count);
        Assert.Single(redoTwo.History.RedoEntries);

        var redoThree = redoTwo.History.Redo(redoTwo.Document);
        Assert.True(redoThree.Result.IsSuccess);
        Assert.Equal(3, redoThree.History.UndoEntries.Count);
        Assert.Empty(redoThree.History.RedoEntries);
        Assert.Equal(finalDocumentJson, DocumentJsonExporter.Serialize(redoThree.Document));
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
}
