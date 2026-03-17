using System.Xml.Linq;

namespace CraftMCP.Tests.Unit.App;

public sealed class MainWindowLayoutTests
{
    [Fact]
    public void MainWindow_UsesReducedStartupFootprintAndRebalancedShellColumns()
    {
        var window = LoadMainWindowRoot();

        Assert.Equal("1360", window.Attribute("Width")?.Value);
        Assert.Equal("840", window.Attribute("Height")?.Value);
        Assert.Equal("1080", window.Attribute("MinWidth")?.Value);

        var shellGrid = window
            .Descendants()
            .First(element => element.Name.LocalName == "Grid"
                && (string?)element.Attribute("Grid.Row") == "1");

        Assert.Equal("300,1.2*,336", shellGrid.Attribute("ColumnDefinitions")?.Value);
    }

    [Fact]
    public void MainWindow_PlacesProposalReviewBeforeCapabilitySurface()
    {
        var xaml = File.ReadAllText(GetMainWindowPath());

        var proposalReviewIndex = xaml.IndexOf("Text=\"Proposal Review\"", StringComparison.Ordinal);
        var capabilitySurfaceIndex = xaml.IndexOf("Text=\"Agents + Capability Surface\"", StringComparison.Ordinal);

        Assert.True(proposalReviewIndex >= 0, "Proposal Review card should exist in MainWindow.axaml.");
        Assert.True(capabilitySurfaceIndex >= 0, "Capability Surface card should exist in MainWindow.axaml.");
        Assert.True(
            proposalReviewIndex < capabilitySurfaceIndex,
            "Proposal review should appear above the capability surface in the left rail.");
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
