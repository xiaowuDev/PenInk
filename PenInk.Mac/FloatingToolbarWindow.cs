using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;

namespace PenInk.Mac;

internal sealed class FloatingToolbarWindow : Window
{
    private readonly FloatingToolbarControl toolbar = new();

    public event Action<ToolMode>? ModeRequested
    {
        add => toolbar.ModeRequested += value;
        remove => toolbar.ModeRequested -= value;
    }

    public event Action? UndoRequested
    {
        add => toolbar.UndoRequested += value;
        remove => toolbar.UndoRequested -= value;
    }

    public event Action? ClearRequested
    {
        add => toolbar.ClearRequested += value;
        remove => toolbar.ClearRequested -= value;
    }

    public event Action<Color>? ColorRequested
    {
        add => toolbar.ColorRequested += value;
        remove => toolbar.ColorRequested -= value;
    }

    public event Action<double>? WidthRequested
    {
        add => toolbar.WidthRequested += value;
        remove => toolbar.WidthRequested -= value;
    }

    public event Action? HideRequested
    {
        add => toolbar.HideRequested += value;
        remove => toolbar.HideRequested -= value;
    }

    public FloatingToolbarWindow()
    {
        Title = "PenInk Tools";
        Width = 72;
        Height = 482;
        MinWidth = 72;
        MinHeight = 482;
        MaxWidth = 72;
        MaxHeight = 482;
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        CanResize = false;
        Content = toolbar;

        Opened += (_, _) => KeepAboveOverlay();
    }

    public void SetState(ToolMode mode, Color color, double width)
    {
        toolbar.SetState(mode, color, width);
        KeepAboveOverlay();
    }

    public void KeepAboveOverlay()
    {
        Topmost = true;
        MacWindowInterop.KeepToolbarAboveOverlay(this);
    }

    public void PlaceNear(Screen? screen)
    {
        if (screen == null)
        {
            Position = new PixelPoint(1280, 220);
            return;
        }

        var area = screen.WorkingArea;
        var widthPx = (int)Math.Round(Width * screen.Scaling);
        var heightPx = (int)Math.Round(Height * screen.Scaling);
        var marginPx = (int)Math.Round(28 * screen.Scaling);
        Position = new PixelPoint(
            area.X + area.Width - widthPx - marginPx,
            area.Y + Math.Max(marginPx, (area.Height - heightPx) / 2));
    }
}

internal sealed class FloatingToolbarControl : Control
{
    private static readonly Color[] Palette =
    [
        Color.Parse("#FF3030"),
        Color.Parse("#FFE100"),
        Color.Parse("#2684FF"),
        Color.Parse("#25B46B"),
        Color.Parse("#FFFFFF"),
        Color.Parse("#111827")
    ];

    private readonly List<ToolbarItem> toolbarItems = [];
    private readonly Typeface typeface = new("Arial");

    private ToolMode mode = ToolMode.Mouse;
    private Color currentColor = Palette[0];
    private double currentWidth = 4;
    private Rect dragHandleBounds;
    private Rect sliderBounds;
    private bool isAdjustingWidth;

    public event Action<ToolMode>? ModeRequested;
    public event Action? UndoRequested;
    public event Action? ClearRequested;
    public event Action<Color>? ColorRequested;
    public event Action<double>? WidthRequested;
    public event Action? HideRequested;

    public FloatingToolbarControl()
    {
        Focusable = true;
        ClipToBounds = false;
        Cursor = new Cursor(StandardCursorType.Arrow);
    }

    public void SetState(ToolMode nextMode, Color color, double width)
    {
        mode = nextMode;
        currentColor = color;
        currentWidth = Math.Clamp(width, 1.5, 24);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        toolbarItems.Clear();

        const double panelWidth = 64;
        const double button = 42;
        var x = (Bounds.Width - panelWidth) / 2;
        var y = 0d;
        var panel = new Rect(x, y, panelWidth, Bounds.Height);

        context.DrawRectangle(new SolidColorBrush(Color.Parse("#EF141C28")), new Pen(new SolidColorBrush(Color.Parse("#384F68")), 1), panel, 14);

        dragHandleBounds = new Rect(x, y, panelWidth, 30);
        DrawDragHandle(context, dragHandleBounds);

        y += 36;
        DrawButton(context, new Rect(x + 11, y, button, button), "P", mode == ToolMode.Pen, () => ModeRequested?.Invoke(ToolMode.Pen));
        y += 48;
        DrawButton(context, new Rect(x + 11, y, button, button), "E", mode == ToolMode.Eraser, () => ModeRequested?.Invoke(ToolMode.Eraser));
        y += 48;
        DrawButton(context, new Rect(x + 11, y, button, button), "Z", false, () => UndoRequested?.Invoke());
        y += 48;
        DrawButton(context, new Rect(x + 11, y, button, button), "C", false, () => ClearRequested?.Invoke());
        y += 55;

        context.DrawEllipse(new SolidColorBrush(currentColor), new Pen(Brushes.White, 2), new Point(x + 32, y + 14), 13, 13);
        y += 35;

        for (var i = 0; i < Palette.Length; i++)
        {
            var col = i % 2;
            var row = i / 2;
            var center = new Point(x + 22 + col * 21, y + 10 + row * 21);
            var color = Palette[i];
            context.DrawEllipse(new SolidColorBrush(color), new Pen(color == currentColor ? Brushes.White : Brushes.Transparent, 2), center, 8, 8);
            toolbarItems.Add(new ToolbarItem(new Rect(center.X - 10, center.Y - 10, 20, 20), _ => ColorRequested?.Invoke(color)));
        }

        y += 73;
        DrawSlider(context, new Rect(x + 22, y, 20, 96));
        y += 108;
        DrawButton(context, new Rect(x + 11, y, button, button), "M", mode == ToolMode.Mouse, () => ModeRequested?.Invoke(ToolMode.Mouse));
        y += 48;
        DrawButton(context, new Rect(x + 11, y, button, button), "H", false, () => HideRequested?.Invoke());
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        if (sliderBounds.Contains(point))
        {
            isAdjustingWidth = true;
            UpdateWidthFromSlider(point.Y);
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        foreach (var item in toolbarItems)
        {
            if (!item.Bounds.Contains(point))
            {
                continue;
            }

            item.Action(point);
            e.Handled = true;
            return;
        }

        if (dragHandleBounds.Contains(point) && TopLevel.GetTopLevel(this) is Window window)
        {
            window.BeginMoveDrag(e);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!isAdjustingWidth)
        {
            return;
        }

        UpdateWidthFromSlider(e.GetPosition(this).Y);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!isAdjustingWidth)
        {
            return;
        }

        isAdjustingWidth = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var nextWidth = Math.Clamp(currentWidth + e.Delta.Y, 1.5, 24);
        WidthRequested?.Invoke(nextWidth);
        e.Handled = true;
    }

    private static void DrawDragHandle(DrawingContext context, Rect rect)
    {
        var brush = new SolidColorBrush(Color.Parse("#9EB5CF"));
        for (var row = 0; row < 2; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                var center = new Point(rect.Center.X - 8 + col * 8, rect.Center.Y - 4 + row * 8);
                context.DrawEllipse(brush, null, center, 2, 2);
            }
        }
    }

    private void DrawButton(DrawingContext context, Rect rect, string label, bool active, Action action)
    {
        var background = active ? Color.Parse("#2563EB") : Color.Parse("#101B2633");
        var border = active ? Color.Parse("#8BC5FF") : Color.Parse("#24425973");
        context.DrawRectangle(new SolidColorBrush(background), new Pen(new SolidColorBrush(border), 1), rect, 9);

        var text = new FormattedText(
            label,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            16,
            Brushes.White);
        context.DrawText(text, new Point(rect.Center.X - text.Width / 2, rect.Center.Y - text.Height / 2));
        toolbarItems.Add(new ToolbarItem(rect, _ => action()));
    }

    private void DrawSlider(DrawingContext context, Rect rect)
    {
        sliderBounds = new Rect(rect.X - 8, rect.Y, rect.Width + 16, rect.Height);
        var track = new Rect(rect.Center.X - 2, rect.Y, 4, rect.Height);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#314258")), null, track, 3);

        var ratio = (currentWidth - 1.5) / (24 - 1.5);
        var knobY = rect.Bottom - ratio * rect.Height;
        var center = new Point(rect.Center.X, knobY);
        context.DrawEllipse(Brushes.White, new Pen(new SolidColorBrush(Color.Parse("#2F80ED")), 2), center, 10, 10);
    }

    private void UpdateWidthFromSlider(double y)
    {
        var clampedY = Math.Clamp(y, sliderBounds.Y, sliderBounds.Bottom);
        var ratio = (sliderBounds.Bottom - clampedY) / sliderBounds.Height;
        var nextWidth = 1.5 + ratio * (24 - 1.5);
        WidthRequested?.Invoke(nextWidth);
    }

    private sealed record ToolbarItem(Rect Bounds, Action<Point> Action);
}
