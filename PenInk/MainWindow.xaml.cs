using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using PenInk.Core.Input;
using PenInk.Infrastructure.Windows;
using PenInk.Inking;

namespace PenInk;

public partial class MainWindow : Window
{
    private static readonly Brush SelectedColorBorder = new SolidColorBrush(Color.FromRgb(234, 242, 255));
    private static readonly Brush NormalColorBorder = Brushes.Transparent;

    private readonly InkHistory history;
    private HotkeyService? hotkeys;
    private PointerTapGuard? activeTap;
    private Button? selectedColorButton;
    private DateTime ignoreMouseTapUntilUtc;

    public MainWindow()
    {
        InitializeComponent();
        selectedColorButton = RedColorButton;
        history = new InkHistory(Ink);
        history.Changed += (_, _) => UpdateUndoState();
        ConfigureWindowBounds();
        ConfigureInk();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.SetToolWindow(hwnd);
        hotkeys = new HotkeyService(hwnd);
        hotkeys.PenRequested += (_, _) => ActivatePen();
        hotkeys.EraserRequested += (_, _) => ActivateEraser();
        hotkeys.MouseRequested += (_, _) => ActivateMousePassthrough();
        hotkeys.ClearRequested += (_, _) => ClearInk();
        hotkeys.UndoRequested += (_, _) => Undo();
        hotkeys.HideRequested += (_, _) => HideOverlay();
        hotkeys.Register();
        ActivateMousePassthrough();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        hotkeys?.Dispose();
    }

    private void ConfigureWindowBounds()
    {
        // 覆盖完整虚拟桌面，兼容多显示器和负坐标屏幕。
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    private void ConfigureInk()
    {
        // InkCanvas 直接接收 Windows Ink/Stylus 输入，比手写采样再绘制更稳定。
        Ink.DefaultDrawingAttributes = CreateDrawingAttributes(Colors.Red, WidthSlider.Value);
        Ink.EraserShape = new EllipseStylusShape(26, 26);
        Ink.EditingMode = InkCanvasEditingMode.Ink;
    }

    private static DrawingAttributes CreateDrawingAttributes(Color color, double width)
    {
        return new DrawingAttributes
        {
            Color = color,
            Width = width,
            Height = width,
            FitToCurve = true,
            IgnorePressure = false,
            IsHighlighter = false,
            StylusTip = StylusTip.Ellipse
        };
    }

    private void ActivatePen()
    {
        Show();
        Activate();
        NativeMethods.SetMousePassthrough(new WindowInteropHelper(this).Handle, false);
        hotkeys?.SetUndoEnabled(true);
        Toolbar.Visibility = Visibility.Visible;
        Ink.EditingMode = InkCanvasEditingMode.Ink;
        UpdateModeButtons();
    }

    private void ActivateEraser()
    {
        Show();
        Activate();
        NativeMethods.SetMousePassthrough(new WindowInteropHelper(this).Handle, false);
        hotkeys?.SetUndoEnabled(true);
        Toolbar.Visibility = Visibility.Visible;
        Ink.EditingMode = InkCanvasEditingMode.EraseByPoint;
        UpdateModeButtons();
    }

    private void ActivateMousePassthrough()
    {
        Show();
        Toolbar.Visibility = Visibility.Collapsed;
        Ink.EditingMode = InkCanvasEditingMode.None;
        // 鼠标穿透时 overlay 不拦截桌面操作，只保留全局热键唤回。
        hotkeys?.SetUndoEnabled(false);
        NativeMethods.SetMousePassthrough(new WindowInteropHelper(this).Handle, true);
        UpdateModeButtons();
    }

    private void HideOverlay()
    {
        Ink.EditingMode = InkCanvasEditingMode.None;
        hotkeys?.SetUndoEnabled(false);
        Hide();
    }

    private void ClearInk()
    {
        if (Ink.Strokes.Count == 0)
        {
            return;
        }
        Ink.Strokes.Clear();
    }

    private void Undo()
    {
        history.Undo();
        UpdateUndoState();
    }

    private void UpdateModeButtons()
    {
        SetButtonActive(PenButton, Ink.EditingMode == InkCanvasEditingMode.Ink);
        SetButtonActive(EraserButton, Ink.EditingMode == InkCanvasEditingMode.EraseByPoint);
        UpdateUndoState();
    }

    private static void SetButtonActive(Button button, bool active)
    {
        button.Tag = active ? "Active" : null;
    }

    private void UpdateUndoState()
    {
        UndoButton.IsEnabled = history.CanUndo;
    }

    private void Ink_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
    {
        activeTap = null;
        UpdateUndoState();
    }

    private void Ink_PreviewStylusDown(object sender, StylusDownEventArgs e)
    {
        if (Ink.EditingMode != InkCanvasEditingMode.Ink)
        {
            return;
        }
        activeTap = PointerTapGuard.Start(ToInkPoint(e.GetPosition(Ink)));
    }

    private void Ink_PreviewStylusUp(object sender, StylusEventArgs e)
    {
        if (Ink.EditingMode != InkCanvasEditingMode.Ink || activeTap == null)
        {
            return;
        }
        var tap = activeTap.Finish(ToInkPoint(e.GetPosition(Ink)));
        activeTap = null;
        ignoreMouseTapUntilUtc = DateTime.UtcNow.AddMilliseconds(120);
        Dispatcher.BeginInvoke(() => AddDotIfNeeded(tap), DispatcherPriority.Background);
    }

    private void Ink_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Ink.EditingMode == InkCanvasEditingMode.Ink && e.StylusDevice == null)
        {
            activeTap = PointerTapGuard.Start(ToInkPoint(e.GetPosition(Ink)));
        }
    }

    private void Ink_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (Ink.EditingMode != InkCanvasEditingMode.Ink
            || activeTap == null
            || e.StylusDevice != null
            || DateTime.UtcNow < ignoreMouseTapUntilUtc)
        {
            return;
        }
        var tap = activeTap.Finish(ToInkPoint(e.GetPosition(Ink)));
        activeTap = null;
        Dispatcher.BeginInvoke(() => AddDotIfNeeded(tap), DispatcherPriority.Background);
    }

    private void AddDotIfNeeded(PointerTap tap)
    {
        // 很短的落笔点可能不会形成 Stroke，这里补一个点，避免写字断笔。
        if (!tap.IsDotCandidate || history.ChangedAfter(tap.StartedUtc))
        {
            return;
        }
        var stroke = new Stroke(new StylusPointCollection
        {
            new StylusPoint(tap.End.X, tap.End.Y, 0.7f)
        })
        {
            DrawingAttributes = Ink.DefaultDrawingAttributes.Clone()
        };
        Ink.Strokes.Add(stroke);
        UpdateUndoState();
    }

    private void Pen_Click(object sender, RoutedEventArgs e) => ActivatePen();

    private void Eraser_Click(object sender, RoutedEventArgs e) => ActivateEraser();

    private void Undo_Click(object sender, RoutedEventArgs e) => Undo();

    private void Clear_Click(object sender, RoutedEventArgs e) => ClearInk();

    private void Mouse_Click(object sender, RoutedEventArgs e) => ActivateMousePassthrough();

    private void Hide_Click(object sender, RoutedEventArgs e) => HideOverlay();

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string hex } button)
        {
            var color = ParseColor(hex);
            Ink.DefaultDrawingAttributes = CreateDrawingAttributes(color, WidthSlider.Value);
            CurrentColorDot.Fill = new SolidColorBrush(color);
            SelectColorButton(button);
        }
    }

    private void SelectColorButton(Button button)
    {
        if (selectedColorButton != null)
        {
            selectedColorButton.BorderBrush = NormalColorBorder;
        }
        button.BorderBrush = SelectedColorBorder;
        selectedColorButton = button;
    }

    private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Ink == null)
        {
            return;
        }
        Ink.DefaultDrawingAttributes = CreateDrawingAttributes(Ink.DefaultDrawingAttributes.Color, e.NewValue);
        Ink.EraserShape = new EllipseStylusShape(e.NewValue * 6, e.NewValue * 6);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            HideOverlay();
            e.Handled = true;
        }
        else if (e.Key == Key.Z
                 && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)
                 && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            Undo();
            e.Handled = true;
        }
    }

    private static Color ParseColor(string hex)
    {
        return Color.FromRgb(
            byte.Parse(hex.AsSpan(1, 2), NumberStyles.HexNumber),
            byte.Parse(hex.AsSpan(3, 2), NumberStyles.HexNumber),
            byte.Parse(hex.AsSpan(5, 2), NumberStyles.HexNumber));
    }

    private static InkPoint ToInkPoint(Point point) => new(point.X, point.Y);
}
