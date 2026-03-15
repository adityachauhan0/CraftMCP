using CraftMCP.App.Models;
using CraftMCP.App.Services;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Tests.Unit.App;

public sealed class WorkspaceDocumentServiceTests
{
    [Fact]
    public void CreateNewDocument_UsesPresetMetadataForCanvas()
    {
        var service = new WorkspaceDocumentService();
        var preset = DocumentPresetDefinition.BuiltIn.Single(definition => definition.Preset == CanvasPreset.MobileFrame);

        var document = service.CreateNewDocument(preset, "Mobile Layout");

        Assert.Equal("Mobile Layout", document.Name);
        Assert.Equal(CanvasPreset.MobileFrame, document.Canvas.Preset);
        Assert.Equal(393, document.Canvas.Width);
        Assert.Equal(852, document.Canvas.Height);
        Assert.Empty(document.RootNodeIds);
        Assert.Empty(document.Nodes);
        Assert.Empty(document.Assets);
    }
}
