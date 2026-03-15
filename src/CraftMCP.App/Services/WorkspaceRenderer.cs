using Avalonia.Media.Imaging;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Persistence.Contracts;
using CraftMCP.Rendering;
using CraftMCP.Rendering.Assets;
using CraftMCP.Rendering.Scene;
using SkiaSharp;

namespace CraftMCP.App.Services;

public class WorkspaceRenderer
{
    private readonly DocumentRenderPlanBuilder _planBuilder;
    private readonly SkiaDocumentRenderer _renderer;

    public WorkspaceRenderer()
        : this(new DocumentRenderPlanBuilder(), new SkiaDocumentRenderer())
    {
    }

    internal WorkspaceRenderer(DocumentRenderPlanBuilder planBuilder, SkiaDocumentRenderer renderer)
    {
        _planBuilder = planBuilder;
        _renderer = renderer;
    }

    public virtual WorkspaceRenderSnapshot Render(
        DocumentState document,
        IReadOnlyDictionary<AssetId, PackagedAssetContent> assets,
        IReadOnlyCollection<NodeId> selectedNodeIds,
        bool showSafeAreaGuides)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(selectedNodeIds);

        var plan = _planBuilder.Build(document);
        var overlay = selectedNodeIds.Count > 0 || showSafeAreaGuides
            ? new RenderOverlayState(selectedNodeIds.ToArray(), showSafeAreaGuides)
            : null;
        var assetSource = new InMemoryRenderAssetSource(
            assets.ToDictionary(entry => entry.Key, entry => entry.Value.Bytes));

        using var renderResult = _renderer.RenderBitmap(plan, assetSource, overlay);
        using var image = SKImage.FromBitmap(renderResult.Bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());
        var bitmap = new Bitmap(stream);

        return new WorkspaceRenderSnapshot(
            plan,
            bitmap,
            renderResult.Warnings.Select(warning => warning.Message).ToArray());
    }
}

public sealed record WorkspaceRenderSnapshot(
    DocumentRenderPlan Plan,
    Bitmap? Bitmap,
    IReadOnlyList<string> Warnings);
