using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace PenInk.Mac;

public sealed class MainWindow : Window
{
    private readonly OverlayCanvas overlay = new();
    private readonly MacHotkeyService hotkeys = new();

    public MainWindow()
    {
        Title = "PenInk";
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        CanResize = false;
        Content = overlay;

        Opened += (_, _) =>
        {
            FitPrimaryScreen();
            hotkeys.PenRequested += (_, _) => Ui(ActivatePen);
            hotkeys.EraserRequested += (_, _) => Ui(ActivateEraser);
            hotkeys.MouseRequested += (_, _) => Ui(ActivateMouseMode);
            hotkeys.ClearRequested += (_, _) => Ui(overlay.Clear);
            hotkeys.UndoRequested += (_, _) => Ui(overlay.Undo);
            hotkeys.HideRequested += (_, _) => Ui(Hide);
            hotkeys.Start();
            ActivateMouseMode();
        };

        Closed += (_, _) => hotkeys.Dispose();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Z && e.KeyModifiers.HasFlag(KeyModifiers.Meta) && e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            overlay.Undo();
            e.Handled = true;
        }
    }

    private void ActivatePen()
    {
        Show();
        Topmost = true;
        overlay.SetMode(ToolMode.Pen);
        overlay.IsHitTestVisible = true;
        Activate();
    }

    private void ActivateEraser()
    {
        Show();
        Topmost = true;
        overlay.SetMode(ToolMode.Eraser);
        overlay.IsHitTestVisible = true;
        Activate();
    }

    private void ActivateMouseMode()
    {
        Show();
        overlay.SetMode(ToolMode.Mouse);
        overlay.IsHitTestVisible = false;
    }

    private void FitPrimaryScreen()
    {
        var screen = Screens.Primary;
        if (screen == null)
        {
            Width = 1440;
            Height = 900;
            return;
        }

        Position = screen.Bounds.Position;
        Width = screen.Bounds.Width / screen.Scaling;
        Height = screen.Bounds.Height / screen.Scaling;
    }

    private void Ui(Action action)
    {
        Dispatcher.UIThread.Post(action);
    }
}
