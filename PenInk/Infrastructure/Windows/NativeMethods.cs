using System.Runtime.InteropServices;

namespace PenInk.Infrastructure.Windows;

internal static class NativeMethods
{
    public const int WmHotkey = 0x0312;
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;

    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020L;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExLayered = 0x00080000L;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(nint hWnd, int id);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    public static void SetToolWindow(nint hwnd)
    {
        SetExtendedStyle(hwnd, style => style | WsExLayered | WsExToolWindow);
    }

    public static void SetMousePassthrough(nint hwnd, bool enabled)
    {
        // WS_EX_TRANSPARENT 让 overlay 在鼠标模式下不拦截底层窗口。
        SetExtendedStyle(hwnd, style =>
        {
            style |= WsExLayered | WsExToolWindow;
            return enabled ? style | WsExTransparent : style & ~WsExTransparent;
        });
    }

    private static void SetExtendedStyle(nint hwnd, Func<long, long> update)
    {
        var current = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        var next = update(current);
        if (next == current)
        {
            return;
        }

        SetWindowLongPtr(hwnd, GwlExStyle, new nint(next));
        SetWindowPos(hwnd, 0, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
    }
}
