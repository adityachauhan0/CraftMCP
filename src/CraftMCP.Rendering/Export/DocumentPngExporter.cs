using CraftMCP.Rendering.Assets;
using CraftMCP.Rendering.Scene;
using SkiaSharp;

namespace CraftMCP.Rendering.Export;

public sealed class DocumentPngExporter
{
    private readonly SkiaDocumentRenderer _renderer;

    public DocumentPngExporter(SkiaDocumentRenderer? renderer = null)
    {
        _renderer = renderer ?? new SkiaDocumentRenderer();
    }

    public PngExportResult Export(DocumentRenderPlan plan, IRenderAssetSource? assetSource = null)
    {
        using var renderResult = _renderer.RenderBitmap(plan, assetSource ?? InMemoryRenderAssetSource.Empty);
        using var image = SKImage.FromBitmap(renderResult.Bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return new PngExportResult(data.ToArray(), renderResult.Warnings.ToArray());
    }
}
