using System.Xml.Linq;

namespace CraftMCP.Tests.Unit.App;

public sealed class MainWindowLayoutTests
{
    [Fact]
    public void MainWindow_UsesPhaseOneShellSpacingAndRebalancedColumns()
    {
        var window = LoadMainWindowRoot();

        Assert.Equal("1360", window.Attribute("Width")?.Value);
        Assert.Equal("840", window.Attribute("Height")?.Value);
        Assert.Equal("1080", window.Attribute("MinWidth")?.Value);

        var shellGrid = window
            .Descendants()
            .First(element => element.Name.LocalName == "Grid"
                && (string?)element.Attribute("Grid.Row") == "1");

        Assert.Equal("12", shellGrid.Attribute("Margin")?.Value);
        Assert.Equal("308,*,332", shellGrid.Attribute("ColumnDefinitions")?.Value);
        Assert.Equal("12", shellGrid.Attribute("ColumnSpacing")?.Value);
    }

    [Fact]
    public void MainWindow_PlacesTruthfulCapabilityMessagingInsidePromptComposer()
    {
        var xaml = File.ReadAllText(GetMainWindowPath());

        var plannerSurfaceIndex = xaml.IndexOf("Text=\"{Binding PlannerSurfaceText}\"", StringComparison.Ordinal);
        var mcpSurfaceIndex = xaml.IndexOf("Text=\"{Binding McpSurfaceText}\"", StringComparison.Ordinal);
        var proposalReviewIndex = xaml.IndexOf("Text=\"Proposal Review\"", StringComparison.Ordinal);

        Assert.True(plannerSurfaceIndex >= 0, "Planner capability text should exist in MainWindow.axaml.");
        Assert.True(mcpSurfaceIndex >= 0, "MCP limitation text should exist in MainWindow.axaml.");
        Assert.True(proposalReviewIndex >= 0, "Proposal Review card should exist in MainWindow.axaml.");
        Assert.True(
            plannerSurfaceIndex < proposalReviewIndex,
            "Truthful planner capability messaging should appear before proposal review.");
        Assert.True(
            mcpSurfaceIndex < proposalReviewIndex,
            "Truthful MCP limitation messaging should appear before proposal review.");
        Assert.DoesNotContain("Text=\"Agents + Capability Surface\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_UsesDedicatedInspectorIntroCopy()
    {
        var xaml = File.ReadAllText(GetMainWindowPath());

        Assert.Contains("Text=\"{Binding InspectorIntroText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding AgentContextSelectionText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding AgentContextSelectionDetailText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding AgentContextProposalImpactText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding AgentContextRecentActivityText}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_UsesPhaseTwoPromptProposalAndHistoryBindings()
    {
        var xaml = File.ReadAllText(GetMainWindowPath());

        Assert.Contains("Text=\"Current target\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding PromptScopeDetailText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ProposalScopeText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ProposalChangeCountText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ProposalChangeSummaryText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ProposalReviewHintText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding PendingProposalBadgeText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ScopeLabel}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_HidesWarningStripWhenNoWarningsExist()
    {
        var xaml = File.ReadAllText(GetMainWindowPath());

        Assert.Contains("IsVisible=\"{Binding HasRenderWarnings}\"", xaml, StringComparison.Ordinal);
    }

    private static XElement LoadMainWindowRoot() => XElement.Load(GetMainWindowPath());

    private static string GetMainWindowPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CraftMCP.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "src", "CraftMCP.App", "MainWindow.axaml");
    }
}
