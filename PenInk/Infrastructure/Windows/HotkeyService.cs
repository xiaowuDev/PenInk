using System.Windows.Input;
using System.Windows.Interop;

namespace PenInk.Infrastructure.Windows;

internal sealed class HotkeyService : IDisposable
{
    private const int PenId = 1;
    private const int MouseId = 2;
    private const int ClearId = 3;
    private const int UndoId = 4;
    private const int EraserId = 5;
    private const int HideId = 6;

    private readonly nint hwnd;
    private readonly HwndSource source;
    private bool undoRegistered;

    public HotkeyService(nint hwnd)
    {
        this.hwnd = hwnd;
        source = HwndSource.FromHwnd(hwnd) ?? throw new InvalidOperationException("Window handle is not ready.");
        source.AddHook(WndProc);
    }

    public event EventHandler? PenRequested;
    public event EventHandler? EraserRequested;
    public event EventHandler? MouseRequested;
    public event EventHandler? ClearRequested;
    public event EventHandler? UndoRequested;
    public event EventHandler? HideRequested;

    public void Register()
    {
        Register(PenId, NativeMethods.ModControl | NativeMethods.ModAlt, Key.P);
        Register(EraserId, NativeMethods.ModControl | NativeMethods.ModAlt, Key.E);
        Register(MouseId, NativeMethods.ModControl | NativeMethods.ModAlt, Key.M);
        Register(ClearId, NativeMethods.ModControl | NativeMethods.ModAlt, Key.Back);
        Register(HideId, NativeMethods.ModControl | NativeMethods.ModAlt, Key.H);
    }

    public void SetUndoEnabled(bool enabled)
    {
        // 只在绘制模式注册撤销热键，并避开系统/应用常用的 Ctrl+Z。
        if (enabled == undoRegistered)
        {
            return;
        }

        if (enabled)
        {
            undoRegistered = Register(UndoId, NativeMethods.ModControl | NativeMethods.ModAlt, Key.Z);
        }
        else
        {
            NativeMethods.UnregisterHotKey(hwnd, UndoId);
            undoRegistered = false;
        }
    }

    public void Dispose()
    {
        source.RemoveHook(WndProc);
        NativeMethods.UnregisterHotKey(hwnd, PenId);
        NativeMethods.UnregisterHotKey(hwnd, EraserId);
        NativeMethods.UnregisterHotKey(hwnd, MouseId);
        NativeMethods.UnregisterHotKey(hwnd, ClearId);
        NativeMethods.UnregisterHotKey(hwnd, UndoId);
        NativeMethods.UnregisterHotKey(hwnd, HideId);
    }

    private bool Register(int id, uint modifiers, Key key)
    {
        return NativeMethods.RegisterHotKey(hwnd, id, modifiers, (uint)KeyInterop.VirtualKeyFromKey(key));
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg != NativeMethods.WmHotkey)
        {
            return 0;
        }

        handled = true;
        switch (wParam.ToInt32())
        {
            case PenId:
                PenRequested?.Invoke(this, EventArgs.Empty);
                break;
            case EraserId:
                EraserRequested?.Invoke(this, EventArgs.Empty);
                break;
            case MouseId:
                MouseRequested?.Invoke(this, EventArgs.Empty);
                break;
            case ClearId:
                ClearRequested?.Invoke(this, EventArgs.Empty);
                break;
            case UndoId:
                UndoRequested?.Invoke(this, EventArgs.Empty);
                break;
            case HideId:
                HideRequested?.Invoke(this, EventArgs.Empty);
                break;
        }

        return 0;
    }
}
