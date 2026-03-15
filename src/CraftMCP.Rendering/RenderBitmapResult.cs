using SkiaSharp;

namespace CraftMCP.Rendering;

public sealed class RenderBitmapResult : IDisposable
{
    public RenderBitmapResult(SKBitmap bitmap, IReadOnlyList<RenderWarning> warnings)
    {
        Bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
        Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
    }

    public SKBitmap Bitmap { get; }

    public IReadOnlyList<RenderWarning> Warnings { get; }

    public void Dispose() => Bitmap.Dispose();
}
