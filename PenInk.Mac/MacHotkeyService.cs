using Avalonia.Input;
using SharpHook;
using SharpHook.Data;

namespace PenInk.Mac;

internal sealed class MacHotkeyService : IDisposable
{
    private readonly IGlobalHook hook = new TaskPoolGlobalHook(globalHookType: GlobalHookType.Keyboard);

    public event EventHandler? PenRequested;
    public event EventHandler? EraserRequested;
    public event EventHandler? MouseRequested;
    public event EventHandler? ClearRequested;
    public event EventHandler? UndoRequested;
    public event EventHandler? HideRequested;

    public void Start()
    {
        hook.KeyPressed += OnKeyPressed;
        hook.RunAsync();
    }

    public void Dispose()
    {
        hook.KeyPressed -= OnKeyPressed;
        hook.Dispose();
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        var mask = e.RawEvent.Mask;
        var command = mask.HasFlag(EventMask.Meta);
        var option = mask.HasFlag(EventMask.Alt);
        if (!command || !option)
        {
            return;
        }

        switch (e.Data.KeyCode)
        {
            case KeyCode.VcP:
                PenRequested?.Invoke(this, EventArgs.Empty);
                break;
            case KeyCode.VcE:
                EraserRequested?.Invoke(this, EventArgs.Empty);
                break;
            case KeyCode.VcM:
                MouseRequested?.Invoke(this, EventArgs.Empty);
                break;
            case KeyCode.VcZ:
                UndoRequested?.Invoke(this, EventArgs.Empty);
                break;
            case KeyCode.VcH:
                HideRequested?.Invoke(this, EventArgs.Empty);
                break;
            case KeyCode.VcBackspace:
            case KeyCode.VcDelete:
                ClearRequested?.Invoke(this, EventArgs.Empty);
                break;
        }
    }
}
