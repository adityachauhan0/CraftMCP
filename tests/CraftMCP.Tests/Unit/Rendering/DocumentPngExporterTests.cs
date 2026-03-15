using CraftMCP.Domain.Ids;
using CraftMCP.Rendering;
using CraftMCP.Rendering.Assets;
using CraftMCP.Rendering.Export;
using CraftMCP.Rendering.Scene;
using CraftMCP.Tests.TestSupport;
using SkiaSharp;

namespace CraftMCP.Tests.Unit.Rendering;

public sealed class DocumentPngExporterTests
{
    [Fact]
    public void Export_UsesCanvasDimensionsAndMatchesSceneRenderWithoutOverlays()
    {
        var document = DocumentExportFixtureFactory.CreateSocialGraphic();
        var plan = new DocumentRenderPlanBuilder().Build(document);
        var renderer = new SkiaDocumentRenderer();
        var exporter = new DocumentPngExporter(renderer);
        var overlay = new RenderOverlayState(new[] { NodeId.From("node_social_cta_button") });

        using var baseResult = renderer.RenderBitmap(plan, InMemoryRenderAssetSource.Empty);
        using var overlayResult = renderer.RenderBitmap(plan, InMemoryRenderAssetSource.Empty, overlay);
        var exportResult = exporter.Export(plan, InMemoryRenderAssetSource.Empty);
        using var exportedBitmap = SKBitmap.Decode(exportResult.PngBytes);

        Assert.Equal(1080, exportedBitmap.Width);
        Assert.Equal(1080, exportedBitmap.Height);
        Assert.Equal(baseResult.Bitmap.GetPixel(640, 424), exportedBitmap.GetPixel(640, 424));
        Assert.NotEqual(overlayResult.Bitmap.GetPixel(640, 424), exportedBitmap.GetPixel(640, 424));
    }
}
