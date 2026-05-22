package com.pencli.book.infrastructure.windows;

import com.pencli.book.application.port.out.OverlayWindowPort;
import com.sun.jna.platform.win32.User32;
import com.sun.jna.platform.win32.WinDef.HWND;
import com.sun.jna.platform.win32.WinUser;
import javafx.stage.Stage;

import java.util.Locale;
import java.util.Objects;

public final class WindowsOverlayWindowAdapter implements OverlayWindowPort {
    private static final int WS_EX_TOOLWINDOW = 0x00000080;

    private final Stage stage;
    private final String windowTitle;
    private final boolean windows;
    private HWND hwnd;

    public WindowsOverlayWindowAdapter(Stage stage, String windowTitle) {
        this.stage = Objects.requireNonNull(stage, "stage");
        this.windowTitle = Objects.requireNonNull(windowTitle, "windowTitle");
        this.windows = System.getProperty("os.name", "").toLowerCase(Locale.ROOT).contains("win");
    }

    @Override
    public void showInteractive() {
        stage.show();
        stage.setAlwaysOnTop(true);
        stage.toFront();
        applyMousePassthrough(false);
    }

    @Override
    public void showPassthrough() {
        stage.show();
        stage.setAlwaysOnTop(true);
        stage.toFront();
        applyMousePassthrough(true);
    }

    @Override
    public void hideOverlay() {
        stage.hide();
    }

    private void applyMousePassthrough(boolean enabled) {
        if (!windows) {
            return;
        }
        HWND window = resolveWindowHandle();
        if (window == null) {
            return;
        }

        int style = User32.INSTANCE.GetWindowLong(window, WinUser.GWL_EXSTYLE);
        style |= WinUser.WS_EX_LAYERED;
        style |= WS_EX_TOOLWINDOW;
        if (enabled) {
            style |= WinUser.WS_EX_TRANSPARENT;
        } else {
            style &= ~WinUser.WS_EX_TRANSPARENT;
        }

        User32.INSTANCE.SetWindowLong(window, WinUser.GWL_EXSTYLE, style);
        User32.INSTANCE.SetWindowPos(
                window,
                null,
                0,
                0,
                0,
                0,
                WinUser.SWP_NOMOVE
                        | WinUser.SWP_NOSIZE
                        | WinUser.SWP_NOZORDER
                        | WinUser.SWP_NOACTIVATE
                        | WinUser.SWP_FRAMECHANGED
        );
    }

    private HWND resolveWindowHandle() {
        if (hwnd != null) {
            return hwnd;
        }
        hwnd = User32.INSTANCE.FindWindow(null, windowTitle);
        return hwnd;
    }
}
