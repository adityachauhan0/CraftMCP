using CraftMCP.Domain.Commands;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.ValueObjects;
using CraftMCP.Tests.TestSupport;

namespace CraftMCP.Tests.Unit.Commands;

public sealed class CommandValidatorTests
{
    [Fact]
    public void Validate_AllowsLaterCommandsToReferenceEarlierCreates()
    {
        var document = DocumentExportFixtureFactory.CreateSlide();
        var nodeId = NodeId.From("node_slide_note_card");
        var batch = CreateHumanBatch(
            "Create a note card and hide it",
            new CreateNodeCommand(CreateRectangleNode(nodeId), 1),
            new SetVisibilityCommand(nodeId, false),
            new SetLockStateCommand(nodeId, true));

        var result = CommandValidator.Validate(document, batch);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_RejectsNodeKindChangesThroughUpdate()
    {
        var document = DocumentExportFixtureFactory.CreateUiMockup();
        var nodeId = NodeId.From("node_ui_title");
        var batch = CreateHumanBatch(
            "Break the node kind",
            new UpdateNodeCommand(new RectangleNode(
                nodeId,
                "Profile Title",
                new TransformValue(580, 116, 1, 1, 0),
                null,
                true,
                false,
                OpacityValue.Full,
                new SizeValue(240, 36),
                new ColorValue(28, 32, 42),
                null,
                0)));

        var result = CommandValidator.Validate(document, batch);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "node_kind_mismatch" && error.NodeId == nodeId);
    }

    [Fact]
    public void Validate_RejectsDuplicateAssetIds()
    {
        var document = DocumentExportFixtureFactory.CreateUiMockup();
        var assetId = AssetId.From("asset_ui_avatar");
        var batch = CreateHumanBatch(
            "Duplicate the avatar asset",
            new ImportAssetCommand(new AssetManifestEntry(
                assetId,
                "avatar-duplicate.png",
                "image/png",
                "hash-ui-avatar-duplicate")));

        var result = CommandValidator.Validate(document, batch);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "asset_already_exists" && error.AssetId == assetId);
    }

    private static CommandBatch CreateHumanBatch(string summary, params DesignCommand[] commands) =>
        new(
            summary,
            commands,
            new CommandProvenance(CommandSource.Human, "user:test"),
            null);

    private static RectangleNode CreateRectangleNode(NodeId id) =>
        new(
            id,
            "Note Card",
            new TransformValue(120, 220, 1, 1, 0),
            null,
            true,
            false,
            OpacityValue.Full,
            new SizeValue(320, 180),
            new ColorValue(255, 255, 255),
            new StrokeStyle(new ColorValue(220, 226, 232), 1),
            16);
}
