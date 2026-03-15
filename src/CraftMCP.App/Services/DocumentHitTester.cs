using Avalonia;
using CraftMCP.App.Models.Session;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Rendering.Scene;

namespace CraftMCP.App.Services;

public sealed class DocumentHitTester
{
    private const double HandleSize = 10d;
    private const double RotateHandleOffset = 28d;

    public Rect GetCanvasScreenRect(Size surfaceSize, CanvasModel canvas, ViewportState viewport)
    {
        var width = canvas.Width * viewport.Zoom;
        var height = canvas.Height * viewport.Zoom;
        var left = ((surfaceSize.Width - width) / 2d) + viewport.PanX;
        var top = ((surfaceSize.Height - height) / 2d) + viewport.PanY;
        return new Rect(left, top, width, height);
    }

    public Point ScreenToCanvas(Size surfaceSize, CanvasModel canvas, ViewportState viewport, Point point)
    {
        var rect = GetCanvasScreenRect(surfaceSize, canvas, viewport);
        return new Point((point.X - rect.X) / viewport.Zoom, (point.Y - rect.Y) / viewport.Zoom);
    }

    public Point CanvasToScreen(Size surfaceSize, CanvasModel canvas, ViewportState viewport, Point point)
    {
        var rect = GetCanvasScreenRect(surfaceSize, canvas, viewport);
        return new Point(rect.X + (point.X * viewport.Zoom), rect.Y + (point.Y * viewport.Zoom));
    }

    public CanvasHitResult HitTest(
        DocumentRenderPlan plan,
        WorkspaceSessionState session,
        Size surfaceSize,
        Point screenPoint)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var canvasRect = GetCanvasScreenRect(surfaceSize, new CanvasModel(plan.Width, plan.Height, CraftMCP.Domain.ValueObjects.CanvasPreset.Custom, plan.Background, plan.SafeArea), session.Viewport);
        if (!canvasRect.Contains(screenPoint))
        {
            return new CanvasHitResult(screenPoint, null, CanvasHandleKind.None, false);
        }

        if (session.Selection.HasSingleSelection)
        {
            var selectedId = session.Selection.PrimaryNodeId!.Value;
            var selectedNode = plan.Nodes.LastOrDefault(node => node.NodeId == selectedId);
            if (selectedNode is not null)
            {
                var handleHit = HitTestHandle(plan, session.Viewport, surfaceSize, selectedNode.Bounds, screenPoint);
                if (handleHit != CanvasHandleKind.None)
                {
                    return new CanvasHitResult(
                        ScreenToCanvas(surfaceSize, new CanvasModel(plan.Width, plan.Height, CraftMCP.Domain.ValueObjects.CanvasPreset.Custom, plan.Background, plan.SafeArea), session.Viewport, screenPoint),
                        selectedId,
                        handleHit,
                        true);
                }
            }
        }

        var canvasPoint = ScreenToCanvas(surfaceSize, new CanvasModel(plan.Width, plan.Height, CraftMCP.Domain.ValueObjects.CanvasPreset.Custom, plan.Background, plan.SafeArea), session.Viewport, screenPoint);
        for (var index = plan.Nodes.Count - 1; index >= 0; index--)
        {
            var node = plan.Nodes[index];
            if (!node.IsDrawable)
            {
                continue;
            }

            if (canvasPoint.X >= node.Bounds.Left
                && canvasPoint.X <= node.Bounds.Right
                && canvasPoint.Y >= node.Bounds.Top
                && canvasPoint.Y <= node.Bounds.Bottom)
            {
                return new CanvasHitResult(canvasPoint, node.NodeId, CanvasHandleKind.Move, true);
            }
        }

        return new CanvasHitResult(canvasPoint, null, CanvasHandleKind.None, true);
    }

    private CanvasHandleKind HitTestHandle(
        DocumentRenderPlan plan,
        ViewportState viewport,
        Size surfaceSize,
        AxisAlignedBounds bounds,
        Point screenPoint)
    {
        var canvas = new CanvasModel(plan.Width, plan.Height, CraftMCP.Domain.ValueObjects.CanvasPreset.Custom, plan.Background, plan.SafeArea);
        var topLeft = CanvasToScreen(surfaceSize, canvas, viewport, new Point(bounds.Left, bounds.Top));
        var topRight = CanvasToScreen(surfaceSize, canvas, viewport, new Point(bounds.Right, bounds.Top));
        var bottomLeft = CanvasToScreen(surfaceSize, canvas, viewport, new Point(bounds.Left, bounds.Bottom));
        var bottomRight = CanvasToScreen(surfaceSize, canvas, viewport, new Point(bounds.Right, bounds.Bottom));
        var rotate = CanvasToScreen(surfaceSize, canvas, viewport, new Point((bounds.Left + bounds.Right) / 2d, bounds.Top));
        rotate = new Point(rotate.X, rotate.Y - RotateHandleOffset);

        if (CreateHandleRect(topLeft).Contains(screenPoint))
        {
            return CanvasHandleKind.TopLeft;
        }

        if (CreateHandleRect(topRight).Contains(screenPoint))
        {
            return CanvasHandleKind.TopRight;
        }

        if (CreateHandleRect(bottomLeft).Contains(screenPoint))
        {
            return CanvasHandleKind.BottomLeft;
        }

        if (CreateHandleRect(bottomRight).Contains(screenPoint))
        {
            return CanvasHandleKind.BottomRight;
        }

        if (CreateHandleRect(rotate).Contains(screenPoint))
        {
            return CanvasHandleKind.Rotate;
        }

        return CanvasHandleKind.None;
    }

    private static Rect CreateHandleRect(Point center) =>
        new(center.X - (HandleSize / 2d), center.Y - (HandleSize / 2d), HandleSize, HandleSize);
}

public sealed record CanvasHitResult(
    Point CanvasPoint,
    NodeId? NodeId,
    CanvasHandleKind HandleKind,
    bool IsWithinCanvas);

public enum CanvasHandleKind
{
    None,
    Move,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Rotate,
}
