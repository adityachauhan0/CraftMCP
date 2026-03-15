using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CraftMCP.App.ViewModels;

namespace CraftMCP.App.Controls;

public sealed class WorkspaceCanvasControl : Control
{
    private WorkspaceViewModel? ViewModel => DataContext as WorkspaceViewModel;

    public WorkspaceCanvasControl()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.FillRectangle(new SolidColorBrush(Color.Parse("#0B1220")), Bounds);

        if (ViewModel?.CanvasBitmap is null)
        {
            return;
        }

        var destination = ViewModel.GetCanvasScreenRect(Bounds.Size);
        var source = new Rect(0, 0, ViewModel.CanvasBitmap.PixelSize.Width, ViewModel.CanvasBitmap.PixelSize.Height);
        context.DrawImage(ViewModel.CanvasBitmap, source, destination);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.Parse("#334155")), 1), destination);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        ViewModel?.OnCanvasPointerPressed(
            e.GetPosition(this),
            e.KeyModifiers,
            e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed);

        e.Pointer.Capture(this);
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        ViewModel?.OnCanvasPointerMoved(e.GetPosition(this));
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        ViewModel?.OnCanvasPointerReleased(e.GetPosition(this));
        e.Pointer.Capture(null);
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        ViewModel?.OnCanvasPointerWheel(e.GetPosition(this), e.Delta);
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        ViewModel?.OnCanvasPointerExited();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ViewModel?.SetSurfaceSize(e.NewSize);
        InvalidateVisual();
    }
}
