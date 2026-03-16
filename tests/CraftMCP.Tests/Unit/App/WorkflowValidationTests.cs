using Avalonia;
using CraftMCP.App.Services;
using CraftMCP.App.ViewModels;
using CraftMCP.Domain.Exports;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Nodes;
using CraftMCP.Persistence.Contracts;
using CraftMCP.Persistence.Packaging;
using CraftMCP.Rendering.Scene;
using CraftMCP.Tests.TestSupport;
using SkiaSharp;

namespace CraftMCP.Tests.Unit.App;

public sealed class WorkflowValidationTests
{
    [Fact]
    public void SocialGraphicFixture_WorkflowSupportsAgentReviewManualRefinementAndExport()
    {
        using var sandbox = new WorkflowSandbox();
        using var viewModel = CreateViewModel();
        viewModel.SetSurfaceSize(new Size(1600, 1200));

        var craftPath = sandbox.CopyCraftFixture("social-graphic.v1.craft");
        var jsonPath = sandbox.Combine("social-graphic.reviewed.json");
        var pngPath = sandbox.Combine("social-graphic.reviewed.png");
        var ctaButtonId = NodeId.From("node_social_cta_button");
        var titleId = NodeId.From("node_social_title");

        viewModel.OpenDocument(craftPath);
        viewModel.SelectLayerNode(ctaButtonId.Value);
        viewModel.PromptText = "Hide the selected node.";

        viewModel.SubmitPrompt();

        Assert.True(viewModel.CanApproveProposal);

        viewModel.ApproveProposal();

        Assert.False(viewModel.Document.Nodes[ctaButtonId].IsVisible);

        viewModel.Undo();
        Assert.True(viewModel.Document.Nodes[ctaButtonId].IsVisible);

        viewModel.Redo();
        Assert.False(viewModel.Document.Nodes[ctaButtonId].IsVisible);

        viewModel.SelectLayerNode(titleId.Value);
        viewModel.TextContent = "Launch week, without the scramble.";

        viewModel.ApplySelectionProperties();
        viewModel.ExportJson(jsonPath);
        viewModel.ExportPng(pngPath);

        var exportedJson = ReadNormalizedText(jsonPath);
        AssertExportArtifacts(viewModel, jsonPath, pngPath);
        Assert.Contains("Launch week, without the scramble.", exportedJson);
        Assert.Contains(viewModel.ActivityEntries, entry => entry.Summary == "Applied agent proposal.");
    }

    [Fact]
    public void UiMockupFixture_WorkflowKeepsLayerHierarchyLegibleAndExportsStructuredJson()
    {
        using var sandbox = new WorkflowSandbox();
        using var viewModel = CreateViewModel();
        viewModel.SetSurfaceSize(new Size(1600, 1200));

        var craftPath = sandbox.CopyCraftFixture("ui-mockup.v1.craft");
        var jsonPath = sandbox.Combine("ui-mockup.reviewed.json");
        var pngPath = sandbox.Combine("ui-mockup.reviewed.png");
        var buttonLabelId = NodeId.From("node_ui_button_label");
        var cardGroupId = NodeId.From("node_ui_card_group");

        viewModel.OpenDocument(craftPath);

        Assert.Collection(
            viewModel.LayerItems,
            item => AssertLayer(item, "Phone Frame", 0),
            item => AssertLayer(item, "Status", 0),
            item => AssertLayer(item, "Profile Title", 0),
            item => AssertLayer(item, "Divider", 0),
            item => AssertLayer(item, "Profile Card", 0),
            item => AssertLayer(item, "Avatar", 1),
            item => AssertLayer(item, "Name", 1),
            item => AssertLayer(item, "Role", 1),
            item => AssertLayer(item, "Action Button", 1),
            item => AssertLayer(item, "Action Label", 1));

        viewModel.SelectLayerNode(buttonLabelId.Value);
        viewModel.TextContent = "Review";
        viewModel.ApplySelectionProperties();

        viewModel.SelectLayerNode(cardGroupId.Value);
        viewModel.PromptText = "Lock selected nodes.";
        viewModel.SubmitPrompt();
        viewModel.ApproveProposal();

        Assert.True(viewModel.Document.Nodes[cardGroupId].IsLocked);

        viewModel.ExportJson(jsonPath);
        viewModel.ExportPng(pngPath);

        var exportedJson = ReadNormalizedText(jsonPath);
        AssertExportArtifacts(viewModel, jsonPath, pngPath);
        Assert.Contains("Review", exportedJson);
        Assert.Contains("\"childNodeIds\"", exportedJson);
        Assert.Contains("\"Profile Card\"", exportedJson);
    }

    [Fact]
    public void SlideFixture_WorkflowSupportsCanvasProposalHumanRevisionAndExport()
    {
        using var sandbox = new WorkflowSandbox();
        using var viewModel = CreateViewModel();
        viewModel.SetSurfaceSize(new Size(1600, 1200));

        var craftPath = sandbox.CopyCraftFixture("slide.v1.craft");
        var jsonPath = sandbox.Combine("slide.reviewed.json");
        var pngPath = sandbox.Combine("slide.reviewed.png");
        var bulletTwoId = NodeId.From("node_slide_bullet_two");

        viewModel.OpenDocument(craftPath);
        viewModel.PromptText = "Update the canvas background to #14213D.";

        viewModel.SubmitPrompt();

        Assert.True(viewModel.CanApproveProposal);

        viewModel.ApproveProposal();

        Assert.Equal("#14213D", viewModel.CanvasBackgroundText);

        viewModel.SelectLayerNode(bulletTwoId.Value);
        viewModel.TextContent = "Stable ordering keeps follow-up agent passes grounded in structure.";
        viewModel.ApplySelectionProperties();
        viewModel.ExportJson(jsonPath);
        viewModel.ExportPng(pngPath);

        var exportedJson = ReadNormalizedText(jsonPath);
        AssertExportArtifacts(viewModel, jsonPath, pngPath);
        Assert.Contains("Stable ordering keeps follow-up agent passes grounded in structure.", exportedJson);
    }

    [Fact]
    public void ReopenReviseAndReExport_PreservesIdsHierarchyAndPackagedAssets()
    {
        using var sandbox = new WorkflowSandbox();
        var sourceCraftPath = sandbox.CopyCraftFixture("social-graphic.v1.craft");
        var revisedCraftPath = sandbox.Combine("social-graphic.revised.craft");
        var firstExportPath = sandbox.Combine("social-graphic.first-pass.json");
        var secondExportPath = sandbox.Combine("social-graphic.second-pass.json");
        var secondPngPath = sandbox.Combine("social-graphic.second-pass.png");
        var subtitleId = NodeId.From("node_social_subtitle");
        var ctaLabelId = NodeId.From("node_social_cta_label");

        DocumentId originalDocumentId;
        IReadOnlyList<NodeId> originalRootIds;
        IReadOnlyList<AssetId> originalAssetIds;

        using (var firstViewModel = CreateViewModel())
        {
            firstViewModel.SetSurfaceSize(new Size(1600, 1200));
            firstViewModel.OpenDocument(sourceCraftPath);

            originalDocumentId = firstViewModel.Document.Id;
            originalRootIds = firstViewModel.Document.RootNodeIds.ToArray();
            originalAssetIds = firstViewModel.Document.Assets.Keys.ToArray();

            firstViewModel.SelectLayerNode(subtitleId.Value);
            firstViewModel.TextContent = "Round-tripped locally for a second export pass.";
            firstViewModel.ApplySelectionProperties();
            firstViewModel.SaveDocument(revisedCraftPath);
            firstViewModel.ExportJson(firstExportPath);
        }

        using var reopenedViewModel = CreateViewModel();
        reopenedViewModel.SetSurfaceSize(new Size(1600, 1200));
        reopenedViewModel.OpenDocument(revisedCraftPath);

        Assert.Equal(originalDocumentId, reopenedViewModel.Document.Id);
        Assert.Equal(originalRootIds, reopenedViewModel.Document.RootNodeIds);
        Assert.Equal(originalAssetIds, reopenedViewModel.Document.Assets.Keys);
        Assert.Equal(
            "Round-tripped locally for a second export pass.",
            Assert.IsType<TextNode>(reopenedViewModel.Document.Nodes[subtitleId]).Content);

        reopenedViewModel.SelectLayerNode(ctaLabelId.Value);
        reopenedViewModel.TextContent = "Export Again";
        reopenedViewModel.ApplySelectionProperties();
        reopenedViewModel.ExportJson(secondExportPath);
        reopenedViewModel.ExportPng(secondPngPath);

        var savedPackage = new CraftPackageReader().Read(revisedCraftPath).Package;
        Assert.Equal(originalDocumentId, savedPackage.Document.Id);
        Assert.Equal(originalRootIds, savedPackage.Document.RootNodeIds);
        Assert.Equal(originalAssetIds, savedPackage.Document.Assets.Keys);

        var reExportedJson = ReadNormalizedText(secondExportPath);
        AssertExportArtifacts(reopenedViewModel, secondExportPath, secondPngPath);
        Assert.Contains("Export Again", reExportedJson);
    }

    private static WorkspaceViewModel CreateViewModel() =>
        new(
            new WorkspaceDocumentService(),
            new WorkspaceCommandDispatcher(),
            new TestWorkspaceRenderer(),
            new DocumentHitTester(),
            new NodeFactory(),
            new WorkspaceAgentService());

    private static void AssertExportArtifacts(WorkspaceViewModel viewModel, string jsonPath, string pngPath)
    {
        Assert.True(File.Exists(jsonPath));
        Assert.True(File.Exists(pngPath));
        Assert.Equal(DocumentJsonExporter.Serialize(viewModel.Document), ReadNormalizedText(jsonPath));

        using var bitmap = SKBitmap.Decode(File.ReadAllBytes(pngPath));
        Assert.NotNull(bitmap);
        Assert.Equal((int)viewModel.Document.Canvas.Width, bitmap.Width);
        Assert.Equal((int)viewModel.Document.Canvas.Height, bitmap.Height);
    }

    private static void AssertLayer(LayerItemViewModel item, string expectedName, int expectedDepth)
    {
        Assert.Equal(expectedName, item.Name);
        Assert.Equal(expectedDepth, item.Depth);
    }

    private static string ReadNormalizedText(string path) =>
        File.ReadAllText(path).Replace("\r\n", "\n");

    private sealed class TestWorkspaceRenderer : WorkspaceRenderer
    {
        private readonly DocumentRenderPlanBuilder _planBuilder = new();

        public override WorkspaceRenderSnapshot Render(
            CraftMCP.Domain.Models.DocumentState document,
            IReadOnlyDictionary<AssetId, PackagedAssetContent> assets,
            IReadOnlyCollection<NodeId> selectedNodeIds,
            bool showSafeAreaGuides)
        {
            return new WorkspaceRenderSnapshot(_planBuilder.Build(document), null, Array.Empty<string>());
        }
    }

    private sealed class WorkflowSandbox : IDisposable
    {
        public WorkflowSandbox()
        {
            RootPath = Path.Combine(Path.GetTempPath(), $"craftmcp-e7-{Guid.NewGuid():N}");
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; }

        public string Combine(string fileName) => Path.Combine(RootPath, fileName);

        public string CopyCraftFixture(string fixtureFileName)
        {
            var destination = Combine(fixtureFileName);
            File.Copy(FixtureFile.CraftPath(fixtureFileName), destination, overwrite: true);
            return destination;
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }
}
