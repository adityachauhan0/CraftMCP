using Avalonia;
using CraftMCP.App.Models.Session;
using CraftMCP.App.Services;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.ValueObjects;
using CraftMCP.Rendering.Scene;

namespace CraftMCP.Tests.Unit.App;

public sealed class DocumentHitTesterTests
{
    [Fact]
    public void HitTest_ReturnsTopmostNodeAndHandle()
    {
        var nodeId = NodeId.From("node_rect");
        var node = new RectangleNode(
            nodeId,
            "Rect",
            new TransformValue(0, 0, 1, 1, 0),
            null,
            true,
            false,
            OpacityValue.Full,
            new SizeValue(100, 100),
            new ColorValue(255, 0, 0),
            null,
            0);
        var plan = new DocumentRenderPlan(
            DocumentId.From("doc_test"),
            400,
            300,
            new ColorValue(255, 255, 255),
            null,
            [new DocumentRenderNode(node, System.Numerics.Matrix3x2.Identity, 1, new AxisAlignedBounds(10, 20, 110, 120), true)]);
        var session = WorkspaceSessionState.Default with
        {
            Selection = new SelectionState([nodeId]),
            Viewport = new ViewportState(1, 0, 0, true),
        };
        var hitTester = new DocumentHitTester();
        var surfaceSize = new Size(800, 600);

        var bodyHit = hitTester.HitTest(plan, session, surfaceSize, hitTester.CanvasToScreen(surfaceSize, new CraftMCP.Domain.Models.CanvasModel(400, 300, CanvasPreset.Custom, new ColorValue(255, 255, 255)), session.Viewport, new Point(50, 50)));
        var handleHit = hitTester.HitTest(plan, session, surfaceSize, hitTester.CanvasToScreen(surfaceSize, new CraftMCP.Domain.Models.CanvasModel(400, 300, CanvasPreset.Custom, new ColorValue(255, 255, 255)), session.Viewport, new Point(10, 20)));

        Assert.Equal(nodeId, bodyHit.NodeId);
        Assert.Equal(CanvasHandleKind.Move, bodyHit.HandleKind);
        Assert.Equal(nodeId, handleHit.NodeId);
        Assert.Equal(CanvasHandleKind.TopLeft, handleHit.HandleKind);
    }
}
