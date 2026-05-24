using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering;
using PenInk.Core.Input;

namespace PenInk.Mac;

internal sealed class OverlayCanvas : Control, ICustomHitTest
{
    private readonly List<InkStroke> strokes = [];
    private readonly Stack<Action> undoStack = [];

    private ToolMode mode = ToolMode.Mouse;
    private List<InkSample>? currentStroke;
    private PointerTapGuard? activeTap;
    private Color currentColor = Color.Parse("#FF3030");
    private double currentWidth = 4;

    public ToolMode Mode => mode;
    public Color CurrentColor => currentColor;
    public double CurrentWidth => currentWidth;

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
    }

    public void SetMode(ToolMode nextMode)
    {
        mode = nextMode;
        Cursor = nextMode == ToolMode.Eraser ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Cross);
#if DEBUG
        Console.WriteLine($"Overlay mode: {mode}");
#endif
        InvalidateVisual();
    }

    public void SetColor(Color color)
    {
        currentColor = color;
        InvalidateVisual();
    }

    public void SetWidth(double width)
    {
        currentWidth = Math.Clamp(width, 1.5, 24);
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
#if DEBUG
        Console.WriteLine($"Overlay press: {mode} {point}");
#endif
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

    private static InkPoint ToInkPoint(Point point) => new(point.X, point.Y);

    public bool HitTest(Point point) => mode != ToolMode.Mouse;
}
