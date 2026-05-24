using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace PenInk.Mac;

public sealed class MainWindow : Window
{
    private readonly OverlayCanvas overlay = new();
    private readonly FloatingToolbarWindow toolbar = new();
    private readonly MacHotkeyService hotkeys = new();
    private bool toolbarShown;

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

        toolbar.ModeRequested += mode => Ui(() =>
        {
            switch (mode)
            {
                case ToolMode.Pen:
                    ActivatePen();
                    break;
                case ToolMode.Eraser:
                    ActivateEraser();
                    break;
                case ToolMode.Mouse:
                    ActivateMouseMode();
                    break;
            }
        });
        toolbar.UndoRequested += () => Ui(overlay.Undo);
        toolbar.ClearRequested += () => Ui(overlay.Clear);
        toolbar.ColorRequested += color => Ui(() =>
        {
            overlay.SetColor(color);
            SyncToolbar();
        });
        toolbar.WidthRequested += width => Ui(() =>
        {
            overlay.SetWidth(width);
            SyncToolbar();
        });
        toolbar.HideRequested += () => Ui(HideOverlay);

        Opened += (_, _) =>
        {
            FitPrimaryScreen();
            hotkeys.PenRequested += (_, _) => Ui(ActivatePen);
            hotkeys.EraserRequested += (_, _) => Ui(ActivateEraser);
            hotkeys.MouseRequested += (_, _) => Ui(ActivateMouseMode);
            hotkeys.ClearRequested += (_, _) => Ui(overlay.Clear);
            hotkeys.UndoRequested += (_, _) => Ui(overlay.Undo);
            hotkeys.HideRequested += (_, _) => Ui(HideOverlay);
            hotkeys.Start();
            ActivateMouseMode();
        };

        Closed += (_, _) =>
        {
            toolbar.Close();
            hotkeys.Dispose();
        };
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
#if DEBUG
        Console.WriteLine("Activate pen");
#endif
        Show();
        Topmost = true;
        MacWindowInterop.SetIgnoresMouseEvents(this, false);
        overlay.SetMode(ToolMode.Pen);
        overlay.IsHitTestVisible = true;
        Activate();
        SyncToolbar();
    }

    private void ActivateEraser()
    {
#if DEBUG
        Console.WriteLine("Activate eraser");
#endif
        Show();
        Topmost = true;
        MacWindowInterop.SetIgnoresMouseEvents(this, false);
        overlay.SetMode(ToolMode.Eraser);
        overlay.IsHitTestVisible = true;
        Activate();
        SyncToolbar();
    }

    private void ActivateMouseMode()
    {
#if DEBUG
        Console.WriteLine("Activate mouse");
#endif
        Show();
        overlay.SetMode(ToolMode.Mouse);
        overlay.IsHitTestVisible = false;
        MacWindowInterop.SetIgnoresMouseEvents(this, true);
        SyncToolbar();
    }

    private void HideOverlay()
    {
        MacWindowInterop.SetIgnoresMouseEvents(this, true);
        Hide();
        SyncToolbar();
    }

    private void SyncToolbar()
    {
        if (!toolbarShown)
        {
            toolbar.PlaceNear(Screens.Primary);
            toolbar.Show();
            toolbarShown = true;
        }

        toolbar.SetState(overlay.Mode, overlay.CurrentColor, overlay.CurrentWidth);
        toolbar.KeepAboveOverlay();
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
