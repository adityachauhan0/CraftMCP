using CraftMCP.Domain.Ids;
using CraftMCP.Rendering;
using CraftMCP.Rendering.Assets;
using CraftMCP.Rendering.Scene;
using CraftMCP.Tests.TestSupport;
using SkiaSharp;

namespace CraftMCP.Tests.Unit.Rendering;

public sealed class SkiaDocumentRendererTests
{
    [Fact]
    public void RenderBitmap_WarnsAndDrawsPlaceholderWhenAnImageAssetIsMissing()
    {
        var package = PersistenceFixtureFactory.CreateSocialGraphicPackage();
        var plan = new DocumentRenderPlanBuilder().Build(package.Document);
        var renderer = new SkiaDocumentRenderer();

        using var result = renderer.RenderBitmap(plan, InMemoryRenderAssetSource.Empty);

        var warning = Assert.Single(result.Warnings, item => item.Code == RenderWarningCode.MissingAsset);
        Assert.Equal(AssetId.From("asset_social_hero"), warning.AssetId);
        Assert.Equal(new SKColor(255, 77, 79), result.Bitmap.GetPixel(180, 300));
    }

    [Fact]
    public void RenderBitmap_AddsSelectionOverlayOnlyWhenRequested()
    {
        var document = DocumentExportFixtureFactory.CreateSocialGraphic();
        var plan = new DocumentRenderPlanBuilder().Build(document);
        var renderer = new SkiaDocumentRenderer();
        var overlay = new RenderOverlayState(new[] { NodeId.From("node_social_cta_button") });

        using var baseResult = renderer.RenderBitmap(plan, InMemoryRenderAssetSource.Empty);
        using var overlayResult = renderer.RenderBitmap(plan, InMemoryRenderAssetSource.Empty, overlay);

        Assert.Equal(new SKColor(34, 34, 34), baseResult.Bitmap.GetPixel(640, 424));
        Assert.Equal(new SKColor(0, 191, 255), overlayResult.Bitmap.GetPixel(640, 424));
    }

    [Fact]
    public void RenderBitmap_ReportsFontFallbackWhenRequestedFamilyIsUnavailable()
    {
        var document = RenderingFixtureFactory.CreateFontFallbackDocument();
        var plan = new DocumentRenderPlanBuilder().Build(document);
        var renderer = new SkiaDocumentRenderer();

        using var result = renderer.RenderBitmap(plan, InMemoryRenderAssetSource.Empty);

        Assert.Contains(result.Warnings, warning => warning.Code == RenderWarningCode.FontFallback);
    }
}
