using System.Text.Json;
using CraftMCP.Domain.Commands;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.Serialization;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Tests.Unit.Commands;

public sealed class CommandContractsTests
{
    [Fact]
    public void DesignCommand_RoundTripsWithPolymorphicTypeInfo()
    {
        DesignCommand original = new SetVisibilityCommand(NodeId.From("node_visibility_target"), false);

        var json = JsonSerializer.Serialize(original, CraftJsonSerializerOptions.Default);
        var restored = JsonSerializer.Deserialize<DesignCommand>(json, CraftJsonSerializerOptions.Default);

        var restoredCommand = Assert.IsType<SetVisibilityCommand>(restored);
        Assert.Equal(original, restoredCommand);
    }

    [Fact]
    public void CommandBatch_RoundTripsWithAgentProvenance()
    {
        var nodeId = NodeId.From("node_profile_card");
        var batch = new CommandBatch(
            "Create profile card and hide divider",
            new DesignCommand[]
            {
                new CreateNodeCommand(
                    new RectangleNode(
                        nodeId,
                        "Profile Card",
                        new TransformValue(560, 220, 1, 1, 0),
                        null,
                        true,
                        false,
                        OpacityValue.Full,
                        new SizeValue(280, 160),
                        new ColorValue(255, 255, 255),
                        new StrokeStyle(new ColorValue(220, 226, 232), 1),
                        18),
                    0),
                new SetVisibilityCommand(NodeId.From("node_ui_divider"), false),
            },
            new CommandProvenance(CommandSource.Agent, "planner:mock", "prompt_profile_refresh", "Converted planner output into a reviewable batch."),
            "Tighten the profile layout and reduce chrome.");

        var json = JsonSerializer.Serialize(batch, CraftJsonSerializerOptions.Default);
        var restored = JsonSerializer.Deserialize<CommandBatch>(json, CraftJsonSerializerOptions.Default);

        Assert.NotNull(restored);
        Assert.Equal(batch.Summary, restored.Summary);
        Assert.Equal(batch.Provenance, restored.Provenance);
        Assert.Equal(batch.Rationale, restored.Rationale);
        Assert.Collection(
            restored.Commands,
            command => Assert.IsType<CreateNodeCommand>(command),
            command => Assert.IsType<SetVisibilityCommand>(command));
    }

    [Fact]
    public void HistoryEntry_CapturesWarningsAndInverseCommands()
    {
        var historyEntry = new HistoryEntry(
            "history_20260315_0001",
            DateTimeOffset.Parse("2026-03-15T09:30:00+05:30"),
            new CommandBatch(
                "Import hero image",
                new DesignCommand[]
                {
                    new ImportAssetCommand(new AssetManifestEntry(
                        AssetId.From("asset_social_hero"),
                        "social-hero.png",
                        "image/png",
                        "hash-social-hero")),
                },
                new CommandProvenance(CommandSource.Human, "user:eden"),
                null),
            new CommandResult(
                true,
                new[]
                {
                    new CommandWarning("asset_reused", "Existing packaged asset was reused for this import.", 0, assetId: AssetId.From("asset_social_hero")),
                },
                Array.Empty<CommandFailure>(),
                Array.Empty<NodeId>(),
                new[] { AssetId.From("asset_social_hero") },
                new DesignCommand[]
                {
                    new DeleteNodeCommand(NodeId.From("node_placeholder_cleanup")),
                }));

        Assert.Equal("history_20260315_0001", historyEntry.EntryId);
        Assert.Single(historyEntry.Result.Warnings);
        Assert.Empty(historyEntry.Result.Errors);
        Assert.Single(historyEntry.Result.InverseCommands);
        Assert.Equal(CommandSource.Human, historyEntry.Batch.Provenance.Source);
    }
}
