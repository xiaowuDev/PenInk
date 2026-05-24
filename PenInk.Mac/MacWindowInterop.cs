using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace PenInk.Mac;

internal static class MacWindowInterop
{
    // NSStatusWindowLevel keeps the small tool window above the full-screen drawing overlay.
    private const nint ToolbarWindowLevel = 25;
    private const string ObjectiveCLibrary = "/usr/lib/libobjc.A.dylib";

    public static void SetIgnoresMouseEvents(Window window, bool ignoresMouseEvents)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        objc_msgSend_bool(handle, sel_registerName("setIgnoresMouseEvents:"), ignoresMouseEvents);
    }

    public static void KeepToolbarAboveOverlay(Window window)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        objc_msgSend_nint(handle, sel_registerName("setLevel:"), ToolbarWindowLevel);
        objc_msgSend(handle, sel_registerName("orderFrontRegardless"));
    }

    [DllImport(ObjectiveCLibrary)]
    private static extern IntPtr sel_registerName([MarshalAs(UnmanagedType.LPUTF8Str)] string selectorName);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_nint(IntPtr receiver, IntPtr selector, nint value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_bool(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.I1)] bool value);
}
