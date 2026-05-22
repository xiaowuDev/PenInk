using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PenInk.Core.Input;

namespace PenInk.Mac;

internal sealed class OverlayCanvas : Control
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

    private readonly List<InkStroke> strokes = [];
    private readonly Stack<Action> undoStack = [];
    private readonly List<ToolbarItem> toolbarItems = [];
    private readonly Typeface typeface = new("Arial");

    private ToolMode mode = ToolMode.Mouse;
    private List<InkSample>? currentStroke;
    private PointerTapGuard? activeTap;
    private Color currentColor = Palette[0];
    private double currentWidth = 4;

    public OverlayCanvas()
    {
        Focusable = true;
        ClipToBounds = false;
        Cursor = new Cursor(StandardCursorType.Cross);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        foreach (var stroke in strokes)
        {
            DrawStroke(context, stroke);
        }

        if (currentStroke is { Count: > 0 })
        {
            DrawStroke(context, new InkStroke(currentStroke, currentColor, currentWidth));
        }

        if (mode != ToolMode.Mouse)
        {
            DrawToolbar(context);
        }
    }

    public void SetMode(ToolMode nextMode)
    {
        mode = nextMode;
        Cursor = nextMode == ToolMode.Eraser ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Cross);
        InvalidateVisual();
    }

    public void Clear()
    {
        if (strokes.Count == 0)
        {
            return;
        }

        var snapshot = strokes.ToList();
        strokes.Clear();
        undoStack.Push(() =>
        {
            strokes.Clear();
            strokes.AddRange(snapshot);
            InvalidateVisual();
        });
        InvalidateVisual();
    }

    public void Undo()
    {
        if (undoStack.TryPop(out var action))
        {
            action();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        if (HandleToolbar(point))
        {
            e.Handled = true;
            return;
        }

        if (mode == ToolMode.Pen)
        {
            BeginStroke(e);
            e.Pointer.Capture(this);
            e.Handled = true;
        }
        else if (mode == ToolMode.Eraser)
        {
            EraseAt(point);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var point = e.GetPosition(this);
        if (mode == ToolMode.Pen && currentStroke != null)
        {
            AppendSample(e);
            e.Handled = true;
        }
        else if (mode == ToolMode.Eraser && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            EraseAt(point);
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (mode == ToolMode.Pen && currentStroke != null)
        {
            FinishStroke(e);
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void BeginStroke(PointerEventArgs e)
    {
        currentStroke = [];
        activeTap = PointerTapGuard.Start(ToInkPoint(e.GetPosition(this)));
        AppendSample(e);
    }

    private void AppendSample(PointerEventArgs e)
    {
        var point = e.GetPosition(this);
        var pressure = Math.Clamp(e.GetCurrentPoint(this).Properties.Pressure, 0.25, 1.0);
        currentStroke?.Add(new InkSample(point, pressure));
        InvalidateVisual();
    }

    private void FinishStroke(PointerEventArgs e)
    {
        var samples = currentStroke ?? [];
        var tap = activeTap?.Finish(ToInkPoint(e.GetPosition(this)));
        currentStroke = null;
        activeTap = null;

        if (samples.Count == 0)
        {
            return;
        }

        if (samples.Count == 1 || tap?.IsDotCandidate == true)
        {
            samples = [samples[^1]];
        }

        var stroke = new InkStroke(samples.ToList(), currentColor, currentWidth);
        strokes.Add(stroke);
        undoStack.Push(() =>
        {
            strokes.Remove(stroke);
            InvalidateVisual();
        });
        InvalidateVisual();
    }

    private void EraseAt(Point point)
    {
        var radius = currentWidth * 4;
        var removed = strokes.Where(stroke => HitsStroke(stroke, point, radius)).ToList();
        if (removed.Count == 0)
        {
            return;
        }

        foreach (var stroke in removed)
        {
            strokes.Remove(stroke);
        }

        undoStack.Push(() =>
        {
            strokes.AddRange(removed);
            InvalidateVisual();
        });
        InvalidateVisual();
    }

    private static bool HitsStroke(InkStroke stroke, Point point, double radius)
    {
        var radiusSquared = radius * radius;
        return stroke.Samples.Any(sample =>
        {
            var dx = sample.Point.X - point.X;
            var dy = sample.Point.Y - point.Y;
            return dx * dx + dy * dy <= radiusSquared;
        });
    }

    private void DrawStroke(DrawingContext context, InkStroke stroke)
    {
        if (stroke.Samples.Count == 1)
        {
            var sample = stroke.Samples[0];
            var size = stroke.Width * sample.Pressure;
            var brush = new SolidColorBrush(stroke.Color);
            context.DrawEllipse(brush, null, sample.Point, size / 2, size / 2);
            return;
        }

        var pen = new Pen(new SolidColorBrush(stroke.Color), stroke.Width, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(stroke.Samples[0].Point, false);
            for (var i = 1; i < stroke.Samples.Count; i++)
            {
                stream.LineTo(stroke.Samples[i].Point);
            }
        }

        context.DrawGeometry(null, pen, geometry);
    }

    private bool HandleToolbar(Point point)
    {
        if (mode == ToolMode.Mouse)
        {
            return false;
        }

        foreach (var item in toolbarItems)
        {
            if (!item.Bounds.Contains(point))
            {
                continue;
            }

            item.Action(point);
            InvalidateVisual();
            return true;
        }

        return false;
    }

    private void DrawToolbar(DrawingContext context)
    {
        toolbarItems.Clear();

        const double panelWidth = 58;
        const double button = 42;
        var x = Bounds.Width - panelWidth - 24;
        var y = (Bounds.Height - 462) / 2;
        var panel = new Rect(x, y, panelWidth, 462);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#EF141C28")), new Pen(Color.Parse("#384F68").ToBrush(), 1), panel, 14);

        y += 14;
        DrawButton(context, new Rect(x + 8, y, button, button), "P", mode == ToolMode.Pen, () => SetMode(ToolMode.Pen));
        y += 48;
        DrawButton(context, new Rect(x + 8, y, button, button), "E", mode == ToolMode.Eraser, () => SetMode(ToolMode.Eraser));
        y += 48;
        DrawButton(context, new Rect(x + 8, y, button, button), "Z", false, Undo);
        y += 48;
        DrawButton(context, new Rect(x + 8, y, button, button), "C", false, Clear);
        y += 55;

        context.DrawEllipse(new SolidColorBrush(currentColor), new Pen(Brushes.White, 2), new Point(x + 29, y + 14), 13, 13);
        y += 35;

        for (var i = 0; i < Palette.Length; i++)
        {
            var col = i % 2;
            var row = i / 2;
            var center = new Point(x + 19 + col * 21, y + 10 + row * 21);
            var color = Palette[i];
            context.DrawEllipse(new SolidColorBrush(color), new Pen(color == currentColor ? Brushes.White : Brushes.Transparent, 2), center, 8, 8);
            toolbarItems.Add(new ToolbarItem(new Rect(center.X - 10, center.Y - 10, 20, 20), _ => currentColor = color));
        }

        y += 73;
        DrawSlider(context, new Rect(x + 19, y, 20, 96));
        y += 108;
        DrawButton(context, new Rect(x + 8, y, button, button), "M", false, () => SetMode(ToolMode.Mouse));
        y += 48;
        DrawButton(context, new Rect(x + 8, y, button, button), "H", false, () =>
        {
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                window.Hide();
            }
        });
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
        var track = new Rect(rect.Center.X - 2, rect.Y, 4, rect.Height);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#314258")), null, track, 3);

        var ratio = (currentWidth - 1.5) / (24 - 1.5);
        var knobY = rect.Bottom - ratio * rect.Height;
        var center = new Point(rect.Center.X, knobY);
        context.DrawEllipse(Brushes.White, new Pen(Color.Parse("#2F80ED").ToBrush(), 2), center, 10, 10);
        toolbarItems.Add(new ToolbarItem(new Rect(rect.X - 8, rect.Y, rect.Width + 16, rect.Height), point => SetWidthFromSlider(rect, point.Y)));
    }

    private void SetWidthFromSlider(Rect rect, double y)
    {
        var clampedY = Math.Clamp(y, rect.Y, rect.Bottom);
        var ratio = (rect.Bottom - clampedY) / rect.Height;
        currentWidth = 1.5 + ratio * (24 - 1.5);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var toolbar = new Rect(Bounds.Width - 90, (Bounds.Height - 462) / 2, 70, 462);
        if (toolbar.Contains(e.GetPosition(this)))
        {
            currentWidth = Math.Clamp(currentWidth + e.Delta.Y, 1.5, 24);
            InvalidateVisual();
            e.Handled = true;
        }
    }

    private static InkPoint ToInkPoint(Point point) => new(point.X, point.Y);

    private sealed record ToolbarItem(Rect Bounds, Action<Point> Action);
}

internal static class ColorExtensions
{
    public static IBrush ToBrush(this Color color) => new SolidColorBrush(color);
}
