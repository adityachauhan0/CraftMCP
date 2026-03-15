using System.Numerics;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.ValueObjects;
using CraftMCP.Rendering.Assets;
using CraftMCP.Rendering.Scene;
using SkiaSharp;

namespace CraftMCP.Rendering;

public sealed class SkiaDocumentRenderer
{
    private static readonly SKColor MissingAssetPlaceholderColor = new(255, 77, 79);
    private static readonly SKColor OverlayColor = new(0, 191, 255);
    private static readonly SKColor SafeAreaGuideColor = new(250, 204, 21, 200);

    public RenderBitmapResult RenderBitmap(
        DocumentRenderPlan plan,
        IRenderAssetSource? assetSource = null,
        RenderOverlayState? overlay = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var bitmap = new SKBitmap((int)Math.Ceiling(plan.Width), (int)Math.Ceiling(plan.Height), true);
        var warnings = new List<RenderWarning>();
        var source = assetSource ?? InMemoryRenderAssetSource.Empty;

        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(ToColor(plan.Background));

        foreach (var node in plan.Nodes)
        {
            if (!node.IsDrawable)
            {
                continue;
            }

            canvas.Save();
            var matrix = ToSkMatrix(node.WorldTransform);
            canvas.Concat(ref matrix);
            DrawNode(canvas, node, source, warnings);
            canvas.Restore();
        }

        if (overlay is not null)
        {
            DrawOverlay(canvas, plan, overlay);
        }

        return new RenderBitmapResult(bitmap, warnings);
    }

    private static void DrawNode(
        SKCanvas canvas,
        DocumentRenderNode renderNode,
        IRenderAssetSource assetSource,
        List<RenderWarning> warnings)
    {
        switch (renderNode.Node)
        {
            case RectangleNode rectangle:
                DrawRectangle(canvas, rectangle, renderNode.EffectiveOpacity);
                break;
            case CircleNode circle:
                DrawCircle(canvas, circle, renderNode.EffectiveOpacity);
                break;
            case LineNode line:
                DrawLine(canvas, line, renderNode.EffectiveOpacity);
                break;
            case TextNode text:
                DrawText(canvas, text, renderNode.EffectiveOpacity, warnings);
                break;
            case ImageNode image:
                DrawImage(canvas, image, renderNode.EffectiveOpacity, assetSource, warnings);
                break;
            default:
                throw new InvalidOperationException($"Unsupported drawable node type '{renderNode.Node.GetType().Name}'.");
        }
    }

    private static void DrawRectangle(SKCanvas canvas, RectangleNode rectangle, double opacity)
    {
        var rect = SKRect.Create(0, 0, (float)rectangle.Size.Width, (float)rectangle.Size.Height);
        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            Color = ApplyOpacity(rectangle.Fill, opacity),
        };

        canvas.DrawRoundRect(rect, (float)rectangle.CornerRadius, (float)rectangle.CornerRadius, fillPaint);

        if (rectangle.Stroke is not null)
        {
            using var strokePaint = CreateStrokePaint(rectangle.Stroke.Value, opacity);
            canvas.DrawRoundRect(rect, (float)rectangle.CornerRadius, (float)rectangle.CornerRadius, strokePaint);
        }
    }

    private static void DrawCircle(SKCanvas canvas, CircleNode circle, double opacity)
    {
        var oval = SKRect.Create(0, 0, (float)circle.Size.Width, (float)circle.Size.Height);
        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            Color = ApplyOpacity(circle.Fill, opacity),
        };

        canvas.DrawOval(oval, fillPaint);

        if (circle.Stroke is not null)
        {
            using var strokePaint = CreateStrokePaint(circle.Stroke.Value, opacity);
            canvas.DrawOval(oval, strokePaint);
        }
    }

    private static void DrawLine(SKCanvas canvas, LineNode line, double opacity)
    {
        using var paint = CreateStrokePaint(line.Stroke, opacity);
        canvas.DrawLine(
            (float)line.Start.X,
            (float)line.Start.Y,
            (float)line.End.X,
            (float)line.End.Y,
            paint);
    }

    private static void DrawText(
        SKCanvas canvas,
        TextNode text,
        double opacity,
        List<RenderWarning> warnings)
    {
        using var typeface = ResolveTypeface(text, warnings);
        using var paint = new SKPaint
        {
            Typeface = typeface,
            TextSize = (float)text.Typography.FontSize,
            IsAntialias = true,
            Color = ApplyOpacity(text.Fill, opacity),
        };

        var alignment = NormalizeAlignment(text.Typography.Alignment);
        paint.TextAlign = alignment switch
        {
            SKTextAlign.Left => SKTextAlign.Left,
            SKTextAlign.Center => SKTextAlign.Center,
            SKTextAlign.Right => SKTextAlign.Right,
            _ => SKTextAlign.Left,
        };

        var bounds = SKRect.Create((float)text.Bounds.X, (float)text.Bounds.Y, (float)text.Bounds.Width, (float)text.Bounds.Height);
        var lines = WrapText(text.Content, paint, bounds.Width);
        var metrics = paint.FontMetrics;
        var baseline = bounds.Top - metrics.Ascent;
        var lineAdvance = (float)(text.Typography.FontSize * text.Typography.LineHeight);

        for (var index = 0; index < lines.Count; index++)
        {
            var y = baseline + (index * lineAdvance);
            if (y > bounds.Bottom - metrics.Descent)
            {
                break;
            }

            var x = alignment switch
            {
                SKTextAlign.Center => bounds.MidX,
                SKTextAlign.Right => bounds.Right,
                _ => bounds.Left,
            };

            canvas.DrawText(lines[index], x, y, paint);
        }
    }

    private static void DrawImage(
        SKCanvas canvas,
        ImageNode imageNode,
        double opacity,
        IRenderAssetSource assetSource,
        List<RenderWarning> warnings)
    {
        if (!assetSource.TryGetBytes(imageNode.Asset.AssetId, out var bytes) || bytes.Length == 0)
        {
            warnings.Add(new RenderWarning(
                RenderWarningCode.MissingAsset,
                $"Asset '{imageNode.Asset.AssetId}' is missing.",
                imageNode.Id,
                imageNode.Asset.AssetId));
            DrawMissingAssetPlaceholder(canvas, imageNode.Bounds, opacity);
            return;
        }

        using var bitmap = SKBitmap.Decode(bytes);
        if (bitmap is null)
        {
            warnings.Add(new RenderWarning(
                RenderWarningCode.InvalidAssetData,
                $"Asset '{imageNode.Asset.AssetId}' could not be decoded.",
                imageNode.Id,
                imageNode.Asset.AssetId));
            DrawMissingAssetPlaceholder(canvas, imageNode.Bounds, opacity);
            return;
        }

        var destination = ToRect(imageNode.Bounds);
        var source = imageNode.Crop is null
            ? new SKRect(0, 0, bitmap.Width, bitmap.Height)
            : ToRect(imageNode.Crop.Value);
        var fitMode = (imageNode.FitMode ?? string.Empty).Trim().ToLowerInvariant();

        using var paint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            Color = new SKColor(255, 255, 255, ToAlpha(opacity)),
        };

        canvas.Save();
        canvas.ClipRect(destination);

        switch (fitMode)
        {
            case "contain":
                canvas.DrawBitmap(bitmap, source, FitRect(source, destination, useMaxScale: false), paint);
                break;
            case "fill":
                canvas.DrawBitmap(bitmap, source, destination, paint);
                break;
            case "cover":
            default:
                canvas.DrawBitmap(bitmap, source, FitRect(source, destination, useMaxScale: true), paint);
                break;
        }

        canvas.Restore();
    }

    private static void DrawOverlay(SKCanvas canvas, DocumentRenderPlan plan, RenderOverlayState overlay)
    {
        if (overlay.ShowSafeAreaGuides && plan.SafeArea is not null)
        {
            var safeArea = new SKRect(
                (float)plan.SafeArea.Value.Left,
                (float)plan.SafeArea.Value.Top,
                (float)(plan.Width - plan.SafeArea.Value.Right),
                (float)(plan.Height - plan.SafeArea.Value.Bottom));

            using var safeAreaPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SafeAreaGuideColor,
                StrokeWidth = 2,
                IsAntialias = true,
            };

            canvas.DrawRect(safeArea, safeAreaPaint);
        }

        if (overlay.SelectedNodeIds.Count == 0)
        {
            return;
        }

        using var outlinePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = OverlayColor,
            StrokeWidth = 2,
            IsAntialias = true,
        };

        using var handlePaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = OverlayColor,
            IsAntialias = true,
        };

        var selectedIds = new HashSet<CraftMCP.Domain.Ids.NodeId>(overlay.SelectedNodeIds);
        foreach (var node in plan.Nodes)
        {
            if (!selectedIds.Contains(node.NodeId))
            {
                continue;
            }

            var rect = new SKRect((float)node.Bounds.Left, (float)node.Bounds.Top, (float)node.Bounds.Right, (float)node.Bounds.Bottom);
            canvas.DrawRect(rect, outlinePaint);

            foreach (var handle in GetHandleRects(rect, 6))
            {
                canvas.DrawRect(handle, handlePaint);
            }
        }
    }

    private static IEnumerable<SKRect> GetHandleRects(SKRect rect, float size)
    {
        var half = size / 2f;
        yield return SKRect.Create(rect.Left - half, rect.Top - half, size, size);
        yield return SKRect.Create(rect.Right - half, rect.Top - half, size, size);
        yield return SKRect.Create(rect.Left - half, rect.Bottom - half, size, size);
        yield return SKRect.Create(rect.Right - half, rect.Bottom - half, size, size);
    }

    private static void DrawMissingAssetPlaceholder(SKCanvas canvas, RectValue bounds, double opacity)
    {
        var rect = ToRect(bounds);
        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = new SKColor(
                MissingAssetPlaceholderColor.Red,
                MissingAssetPlaceholderColor.Green,
                MissingAssetPlaceholderColor.Blue,
                ToAlpha(opacity)),
            IsAntialias = true,
        };
        using var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = new SKColor(127, 29, 29, ToAlpha(opacity)),
            StrokeWidth = 4,
            IsAntialias = true,
        };

        canvas.DrawRect(rect, fillPaint);
        canvas.DrawRect(rect, strokePaint);
        canvas.DrawLine(rect.Left, rect.Top, rect.Right, rect.Bottom, strokePaint);
        canvas.DrawLine(rect.Right, rect.Top, rect.Left, rect.Bottom, strokePaint);
    }

    private static SKTypeface ResolveTypeface(TextNode text, List<RenderWarning> warnings)
    {
        var fontWeight = text.Typography.Weight >= 700 ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        var typeface = SKTypeface.FromFamilyName(text.Typography.FontFamily, fontWeight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            ?? SKTypeface.Default;

        if (!string.Equals(typeface.FamilyName, text.Typography.FontFamily, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(new RenderWarning(
                RenderWarningCode.FontFallback,
                $"Font '{text.Typography.FontFamily}' is unavailable. Using '{typeface.FamilyName}'.",
                text.Id,
                RequestedFontFamily: text.Typography.FontFamily,
                ResolvedFontFamily: typeface.FamilyName));
        }

        return typeface;
    }

    private static IReadOnlyList<string> WrapText(string text, SKPaint paint, float maxWidth)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        var lines = new List<string>();
        foreach (var paragraph in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            var currentLine = words[0];
            for (var index = 1; index < words.Length; index++)
            {
                var candidate = $"{currentLine} {words[index]}";
                if (paint.MeasureText(candidate) <= maxWidth)
                {
                    currentLine = candidate;
                    continue;
                }

                lines.Add(currentLine);
                currentLine = words[index];
            }

            lines.Add(currentLine);
        }

        return lines;
    }

    private static SKRect FitRect(SKRect source, SKRect destination, bool useMaxScale)
    {
        var scaleX = destination.Width / source.Width;
        var scaleY = destination.Height / source.Height;
        var scale = useMaxScale ? Math.Max(scaleX, scaleY) : Math.Min(scaleX, scaleY);
        var width = source.Width * scale;
        var height = source.Height * scale;
        var left = destination.Left + ((destination.Width - width) / 2f);
        var top = destination.Top + ((destination.Height - height) / 2f);
        return SKRect.Create(left, top, width, height);
    }

    private static SKPaint CreateStrokePaint(StrokeStyle stroke, double opacity) =>
        new()
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)stroke.Width,
            IsAntialias = true,
            Color = ApplyOpacity(stroke.Color, opacity),
        };

    private static SKTextAlign NormalizeAlignment(string alignment) =>
        alignment.Trim().ToLowerInvariant() switch
        {
            "center" or "middle" => SKTextAlign.Center,
            "end" or "right" => SKTextAlign.Right,
            _ => SKTextAlign.Left,
        };

    private static SKRect ToRect(RectValue rect) =>
        SKRect.Create((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);

    private static SKColor ApplyOpacity(ColorValue color, double opacity) =>
        new(color.Red, color.Green, color.Blue, (byte)Math.Clamp(Math.Round(color.Alpha * opacity), 0, 255));

    private static SKColor ToColor(ColorValue color) =>
        new(color.Red, color.Green, color.Blue, color.Alpha);

    private static byte ToAlpha(double opacity) =>
        (byte)Math.Clamp(Math.Round(255 * opacity), 0, 255);

    private static SKMatrix ToSkMatrix(Matrix3x2 matrix) =>
        new()
        {
            ScaleX = matrix.M11,
            SkewX = matrix.M21,
            TransX = matrix.M31,
            SkewY = matrix.M12,
            ScaleY = matrix.M22,
            TransY = matrix.M32,
            Persp0 = 0,
            Persp1 = 0,
            Persp2 = 1,
        };
}
