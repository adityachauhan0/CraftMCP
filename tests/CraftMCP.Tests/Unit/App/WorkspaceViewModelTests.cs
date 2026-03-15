using Avalonia;
using Avalonia.Input;
using CraftMCP.App.Models;
using CraftMCP.App.Models.Session;
using CraftMCP.App.Services;
using CraftMCP.App.ViewModels;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.ValueObjects;
using CraftMCP.Persistence.Contracts;
using CraftMCP.Rendering.Scene;

namespace CraftMCP.Tests.Unit.App;

public sealed class WorkspaceViewModelTests
{
    [Fact]
    public void CreateNewDocument_ResetsSessionState()
    {
        using var viewModel = CreateViewModel();
        viewModel.SetSurfaceSize(new Size(1200, 800));
        viewModel.SelectTool(ToolMode.CreateRectangle);

        var preset = DocumentPresetDefinition.BuiltIn.Single(definition => definition.Preset == CanvasPreset.Slide);
        viewModel.CreateNewDocument(preset, "Deck");

        Assert.Equal("Deck", viewModel.Document.Name);
        Assert.Equal(CanvasPreset.Slide, viewModel.Document.Canvas.Preset);
        Assert.False(viewModel.HasSelection);
        Assert.Equal(ToolMode.Select, viewModel.SessionState.ToolMode);
    }

    [Fact]
    public void ApplyCanvasProperties_UpdatesDocumentThroughHistory()
    {
        using var viewModel = CreateViewModel();
        viewModel.SetSurfaceSize(new Size(1200, 800));
        viewModel.CanvasWidthText = "1920";
        viewModel.CanvasHeightText = "1080";
        viewModel.CanvasBackgroundText = "#101820";

        viewModel.ApplyCanvasProperties();

        Assert.Equal(1920, viewModel.Document.Canvas.Width);
        Assert.Equal(1080, viewModel.Document.Canvas.Height);
        Assert.Equal(CanvasPreset.Custom, viewModel.Document.Canvas.Preset);
        Assert.True(viewModel.IsDirty);
        Assert.True(viewModel.CanUndo);
    }

    [Fact]
    public void RectangleCreateFlow_AddsNodeAndSelectsIt()
    {
        using var viewModel = CreateViewModel();
        viewModel.SetSurfaceSize(new Size(1200, 800));
        viewModel.SelectTool(ToolMode.CreateRectangle);

        viewModel.OnCanvasPointerPressed(new Point(200, 200), KeyModifiers.None, false);
        viewModel.OnCanvasPointerReleased(new Point(340, 320));

        var createdNode = Assert.IsType<RectangleNode>(Assert.Single(viewModel.Document.Nodes.Values));
        Assert.Single(viewModel.Document.RootNodeIds);
        Assert.Equal(createdNode.Id, viewModel.Document.RootNodeIds[0]);
        Assert.True(viewModel.HasSelection);
        Assert.Equal(createdNode.Id, viewModel.SessionState.Selection.PrimaryNodeId);
    }

    private static WorkspaceViewModel CreateViewModel() =>
        new(
            new WorkspaceDocumentService(),
            new WorkspaceCommandDispatcher(),
            new TestWorkspaceRenderer(),
            new DocumentHitTester(),
            new NodeFactory());

    private sealed class TestWorkspaceRenderer : WorkspaceRenderer
    {
        private readonly DocumentRenderPlanBuilder _planBuilder = new();

        public override WorkspaceRenderSnapshot Render(
            CraftMCP.Domain.Models.DocumentState document,
            IReadOnlyDictionary<CraftMCP.Domain.Ids.AssetId, PackagedAssetContent> assets,
            IReadOnlyCollection<CraftMCP.Domain.Ids.NodeId> selectedNodeIds,
            bool showSafeAreaGuides)
        {
            return new WorkspaceRenderSnapshot(_planBuilder.Build(document), null, Array.Empty<string>());
        }
    }
}
