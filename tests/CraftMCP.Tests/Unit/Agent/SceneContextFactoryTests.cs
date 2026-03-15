using CraftMCP.Agent;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Nodes;
using CraftMCP.Tests.TestSupport;

namespace CraftMCP.Tests.Unit.Agent;

public sealed class SceneContextFactoryTests
{
    [Fact]
    public void Create_IncludesPromptCanvasSelectionHierarchyAndExportSafeNodeSummaries()
    {
        var document = DocumentExportFixtureFactory.CreateUiMockup();

        var context = SceneContextFactory.Create(
            document,
            "Hide the divider and inspect the card.",
            [NodeId.From("node_ui_divider"), NodeId.From("node_ui_card_group")]);

        Assert.Equal("Hide the divider and inspect the card.", context.Prompt);
        Assert.Equal(1440, context.Canvas.Width);
        Assert.Equal(900, context.Canvas.Height);
        Assert.Equal(NodeId.From("node_ui_card_group"), context.Selection.PrimaryNodeId);
        Assert.Equal(2, context.Selection.SelectedNodeIds.Count);
        Assert.Equal(document.RootNodeIds.Count, context.Hierarchy.Count);

        var groupSummary = Assert.Single(context.NodeSummaries.Where(summary => summary.NodeId == NodeId.From("node_ui_card_group")));
        Assert.Equal(NodeKind.Group, groupSummary.Kind);
        Assert.Equal(5, groupSummary.ChildNodeIds.Count);

        var dividerSummary = Assert.Single(context.NodeSummaries.Where(summary => summary.NodeId == NodeId.From("node_ui_divider")));
        Assert.Equal(NodeKind.Line, dividerSummary.Kind);
        Assert.Equal(280, dividerSummary.End?.X);
        Assert.Null(dividerSummary.TextContent);
        Assert.Null(dividerSummary.AssetId);
    }
}
