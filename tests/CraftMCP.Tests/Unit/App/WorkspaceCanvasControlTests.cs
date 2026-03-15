using System.ComponentModel;
using Avalonia;
using Avalonia.Input;
using CraftMCP.Agent;
using CraftMCP.App.Controls;
using CraftMCP.App.Models;
using CraftMCP.App.Services;
using CraftMCP.App.ViewModels;
using CraftMCP.Domain.Commands;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.ValueObjects;
using CraftMCP.Persistence.Contracts;
using CraftMCP.Rendering.Scene;

namespace CraftMCP.Tests.Unit.App;

public sealed class WorkspaceCanvasControlTests
{
    [Fact]
    public void ApplyCanvasProperties_InvalidatesCanvasVisual()
    {
        using var viewModel = CreateViewModel();
        viewModel.SetSurfaceSize(new Size(1200, 800));
        var control = new TestWorkspaceCanvasControl
        {
            DataContext = viewModel,
        };

        control.ResetInvalidationCount();
        viewModel.CanvasBackgroundText = "#101820";

        viewModel.ApplyCanvasProperties();

        Assert.True(control.InvalidationCount > 0);
    }

    [Fact]
    public void ApproveCanvasProposal_InvalidatesCanvasVisual()
    {
        using var viewModel = CreateViewModel(new CanvasPlanner());
        viewModel.SetSurfaceSize(new Size(1200, 800));
        var control = new TestWorkspaceCanvasControl
        {
            DataContext = viewModel,
        };

        control.ResetInvalidationCount();
        viewModel.PromptText = "Make the canvas dark.";
        viewModel.SubmitPrompt();

        control.ResetInvalidationCount();
        viewModel.ApproveProposal();

        Assert.True(control.InvalidationCount > 0);
    }

    private static WorkspaceViewModel CreateViewModel(IInternalPlanner? planner = null) =>
        new(
            new WorkspaceDocumentService(),
            new WorkspaceCommandDispatcher(),
            new TestWorkspaceRenderer(),
            new DocumentHitTester(),
            new NodeFactory(),
            new WorkspaceAgentService(planner ?? new CanvasPlanner()));

    private sealed class TestWorkspaceRenderer : WorkspaceRenderer
    {
        private readonly DocumentRenderPlanBuilder _planBuilder = new();

        public override WorkspaceRenderSnapshot Render(
            DocumentState document,
            IReadOnlyDictionary<CraftMCP.Domain.Ids.AssetId, PackagedAssetContent> assets,
            IReadOnlyCollection<CraftMCP.Domain.Ids.NodeId> selectedNodeIds,
            bool showSafeAreaGuides)
        {
            return new WorkspaceRenderSnapshot(_planBuilder.Build(document), null, Array.Empty<string>());
        }
    }

    private sealed class CanvasPlanner : IInternalPlanner
    {
        public PlannerOutput Plan(SceneContext context)
        {
            var currentCanvas = context.Canvas;
            var updatedCanvas = new CanvasModel(
                currentCanvas.Width,
                currentCanvas.Height,
                CanvasPreset.Custom,
                new ColorValue(16, 24, 32),
                currentCanvas.SafeArea);
            var batch = new CommandBatch(
                "Update canvas background",
                [new SetCanvasCommand(updatedCanvas)],
                new CommandProvenance(CommandSource.Agent, "planner:test", "proposal_review"),
                "Update the canvas background after approval.");

            return new PlannerOutput(
                "proposal_review",
                PlannerOutputStatus.Ready,
                "Update canvas background",
                "Update the canvas background after approval.",
                batch,
                Array.Empty<CommandWarning>(),
                Array.Empty<CommandFailure>());
        }
    }

    private sealed class TestWorkspaceCanvasControl : WorkspaceCanvasControl
    {
        public int InvalidationCount { get; private set; }

        protected override void InvalidateCanvasVisual()
        {
            InvalidationCount++;
        }

        public void ResetInvalidationCount()
        {
            InvalidationCount = 0;
        }
    }
}
