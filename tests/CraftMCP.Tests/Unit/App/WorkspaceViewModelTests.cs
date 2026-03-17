using CraftMCP.Agent;
using Avalonia;
using Avalonia.Input;
using CraftMCP.App.Models;
using CraftMCP.App.Models.Session;
using CraftMCP.App.Services;
using CraftMCP.App.ViewModels;
using CraftMCP.Domain.Commands;
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
    public void CreateNewDocument_ClearsPreviousActivityEntries()
    {
        using var viewModel = CreateViewModel();
        viewModel.SetSurfaceSize(new Size(1200, 800));
        viewModel.SelectTool(ToolMode.CreateRectangle);
        viewModel.OnCanvasPointerPressed(new Point(200, 200), KeyModifiers.None, false);
        viewModel.OnCanvasPointerReleased(new Point(340, 320));

        var preset = DocumentPresetDefinition.BuiltIn.Single(definition => definition.Preset == CanvasPreset.Slide);
        viewModel.CreateNewDocument(preset, "Deck");

        var entry = Assert.Single(viewModel.ActivityEntries);
        Assert.Equal("Created new document.", entry.Summary);
        Assert.Equal("Presentation Slide", entry.Detail);
    }

    [Fact]
    public void CreateNewDocument_ClearsStaleDirtyMarkerFromDocumentTitle()
    {
        using var viewModel = CreateViewModel();
        viewModel.SetSurfaceSize(new Size(1200, 800));
        viewModel.CanvasBackgroundText = "#101820";

        viewModel.ApplyCanvasProperties();

        Assert.Contains('*', viewModel.DocumentTitle);

        var preset = DocumentPresetDefinition.BuiltIn.Single(definition => definition.Preset == CanvasPreset.SquarePost);
        viewModel.CreateNewDocument(preset, "Fresh Social");

        Assert.False(viewModel.IsDirty);
        Assert.Equal("Fresh Social", viewModel.DocumentTitle);
        Assert.DoesNotContain('*', viewModel.DocumentTitle);
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

    [Fact]
    public void SubmitPrompt_CreatesProposalWithoutMutatingDocument()
    {
        using var viewModel = CreateViewModel(new ReviewPlanner());
        viewModel.SetSurfaceSize(new Size(1200, 800));
        viewModel.SelectTool(ToolMode.CreateRectangle);
        viewModel.OnCanvasPointerPressed(new Point(200, 200), KeyModifiers.None, false);
        viewModel.OnCanvasPointerReleased(new Point(340, 320));
        var node = Assert.Single(viewModel.Document.Nodes.Values);
        var wasDirty = viewModel.IsDirty;
        var couldUndo = viewModel.CanUndo;

        viewModel.PromptText = "Hide the selected node.";

        viewModel.SubmitPrompt();

        Assert.NotNull(viewModel.CurrentProposal);
        Assert.Equal(PlannerOutputStatus.Ready, viewModel.CurrentProposal.Status);
        Assert.True(viewModel.Document.Nodes[node.Id].IsVisible);
        Assert.Equal(wasDirty, viewModel.IsDirty);
        Assert.Equal(couldUndo, viewModel.CanUndo);
    }

    [Fact]
    public void RejectProposal_ClearsPendingReviewWithoutMutatingDocument()
    {
        using var viewModel = CreateViewModel(new ReviewPlanner());
        viewModel.SetSurfaceSize(new Size(1200, 800));
        viewModel.SelectTool(ToolMode.CreateRectangle);
        viewModel.OnCanvasPointerPressed(new Point(200, 200), KeyModifiers.None, false);
        viewModel.OnCanvasPointerReleased(new Point(340, 320));
        var node = Assert.Single(viewModel.Document.Nodes.Values);
        var wasDirty = viewModel.IsDirty;
        var couldUndo = viewModel.CanUndo;
        viewModel.PromptText = "Hide the selected node.";
        viewModel.SubmitPrompt();

        viewModel.RejectProposal();

        Assert.Null(viewModel.CurrentProposal);
        Assert.True(viewModel.Document.Nodes[node.Id].IsVisible);
        Assert.Equal(wasDirty, viewModel.IsDirty);
        Assert.Equal(couldUndo, viewModel.CanUndo);
    }

    [Fact]
    public void ApproveProposal_AppliesAgentBatchThroughHistoryAndActivityLog()
    {
        using var viewModel = CreateViewModel(new ReviewPlanner());
        viewModel.SetSurfaceSize(new Size(1200, 800));
        viewModel.SelectTool(ToolMode.CreateRectangle);
        viewModel.OnCanvasPointerPressed(new Point(200, 200), KeyModifiers.None, false);
        viewModel.OnCanvasPointerReleased(new Point(340, 320));
        var node = Assert.Single(viewModel.Document.Nodes.Values);
        viewModel.PromptText = "Hide the selected node.";
        viewModel.SubmitPrompt();

        viewModel.ApproveProposal();

        Assert.Null(viewModel.CurrentProposal);
        Assert.False(viewModel.Document.Nodes[node.Id].IsVisible);
        Assert.True(viewModel.IsDirty);
        Assert.True(viewModel.CanUndo);
        Assert.Equal("Agent", viewModel.ActivityEntries[0].SourceLabel);
        Assert.Equal("planner:test", viewModel.ActivityEntries[0].Actor);
    }

    [Fact]
    public void RenderWarnings_AreCollapsedWhenNoWarningsExist()
    {
        using var viewModel = CreateViewModel();
        viewModel.SetSurfaceSize(new Size(1200, 800));

        Assert.False(viewModel.HasRenderWarnings);
        Assert.Equal(string.Empty, viewModel.RenderWarningsText);
    }

    [Fact]
    public void RenderWarnings_AreVisibleWhenWarningsExist()
    {
        using var viewModel = CreateViewModel(renderer: new WarningWorkspaceRenderer("Missing font fallback."));
        viewModel.SetSurfaceSize(new Size(1200, 800));

        Assert.True(viewModel.HasRenderWarnings);
        Assert.Equal("Missing font fallback.", viewModel.RenderWarningsText);
    }

    private static WorkspaceViewModel CreateViewModel(
        IInternalPlanner? planner = null,
        WorkspaceRenderer? renderer = null) =>
        new(
            new WorkspaceDocumentService(),
            new WorkspaceCommandDispatcher(),
            renderer ?? new TestWorkspaceRenderer(),
            new DocumentHitTester(),
            new NodeFactory(),
            new WorkspaceAgentService(planner ?? new ReviewPlanner()));

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

    private sealed class ReviewPlanner : IInternalPlanner
    {
        public PlannerOutput Plan(SceneContext context)
        {
            var selectedNodeId = Assert.Single(context.Selection.SelectedNodeIds);
            var batch = new CommandBatch(
                "Hide selected node",
                [new SetVisibilityCommand(selectedNodeId, false)],
                new CommandProvenance(CommandSource.Agent, "planner:test", "proposal_review"),
                "Hide the currently selected node after human approval.");

            return new PlannerOutput(
                "proposal_review",
                PlannerOutputStatus.Ready,
                "Hide selected node",
                "Hide the currently selected node after human approval.",
                batch,
                Array.Empty<CommandWarning>(),
                Array.Empty<CommandFailure>());
        }
    }

    private sealed class WarningWorkspaceRenderer(string warning) : WorkspaceRenderer
    {
        private readonly DocumentRenderPlanBuilder _planBuilder = new();

        public override WorkspaceRenderSnapshot Render(
            CraftMCP.Domain.Models.DocumentState document,
            IReadOnlyDictionary<CraftMCP.Domain.Ids.AssetId, PackagedAssetContent> assets,
            IReadOnlyCollection<CraftMCP.Domain.Ids.NodeId> selectedNodeIds,
            bool showSafeAreaGuides)
        {
            return new WorkspaceRenderSnapshot(_planBuilder.Build(document), null, [warning]);
        }
    }
}
