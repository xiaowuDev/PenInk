package com.pencli.book.adapter.in.hotkey;

import com.github.kwhat.jnativehook.GlobalScreen;
import com.github.kwhat.jnativehook.NativeHookException;
import com.github.kwhat.jnativehook.NativeInputEvent;
import com.github.kwhat.jnativehook.keyboard.NativeKeyEvent;
import com.github.kwhat.jnativehook.keyboard.NativeKeyListener;
import com.pencli.book.application.port.in.HotkeyCommand;
import com.pencli.book.application.port.out.HotkeyPort;

import java.util.EnumMap;
import java.util.Map;
import java.util.Objects;
import java.util.function.Consumer;
import java.util.logging.Level;
import java.util.logging.Logger;

public final class GlobalHotkeyAdapter implements HotkeyPort, NativeKeyListener {
    private static final long REPEAT_GUARD_NANOS = 250_000_000L;

    private final Consumer<HotkeyCommand> commandConsumer;
    private final Map<HotkeyCommand, Long> lastFireTimes = new EnumMap<>(HotkeyCommand.class);
    private boolean started;

    public GlobalHotkeyAdapter(Consumer<HotkeyCommand> commandConsumer) {
        this.commandConsumer = Objects.requireNonNull(commandConsumer, "commandConsumer");
    }

    @Override
    public void start() {
        if (started) {
            return;
        }
        try {
            Logger logger = Logger.getLogger(GlobalScreen.class.getPackageName());
            logger.setUseParentHandlers(false);
            logger.setLevel(Level.OFF);
            if (!GlobalScreen.isNativeHookRegistered()) {
                GlobalScreen.registerNativeHook();
            }
            GlobalScreen.addNativeKeyListener(this);
            started = true;
        } catch (NativeHookException exception) {
            throw new IllegalStateException("Unable to register global hotkeys", exception);
        }
    }

    @Override
    public void stop() {
        if (!started) {
            return;
        }
        GlobalScreen.removeNativeKeyListener(this);
        try {
            if (GlobalScreen.isNativeHookRegistered()) {
                GlobalScreen.unregisterNativeHook();
            }
        } catch (NativeHookException exception) {
            throw new IllegalStateException("Unable to unregister global hotkeys", exception);
        } finally {
            started = false;
        }
    }

    @Override
    public void nativeKeyPressed(NativeKeyEvent event) {
        int keyCode = event.getKeyCode();
        boolean ctrl = hasModifier(event, NativeInputEvent.CTRL_MASK);
        boolean alt = hasModifier(event, NativeInputEvent.ALT_MASK);

        if (ctrl && alt && keyCode == NativeKeyEvent.VC_P) {
            fire(HotkeyCommand.ACTIVATE_PEN);
        } else if (ctrl && alt && keyCode == NativeKeyEvent.VC_M) {
            fire(HotkeyCommand.ACTIVATE_MOUSE_PASSTHROUGH);
        } else if (ctrl && alt && keyCode == NativeKeyEvent.VC_C) {
            fire(HotkeyCommand.CLEAR);
        } else if (ctrl && !alt && keyCode == NativeKeyEvent.VC_Z) {
            fire(HotkeyCommand.UNDO);
        } else if (keyCode == NativeKeyEvent.VC_ESCAPE) {
            fire(HotkeyCommand.HIDE);
        }
    }

    private static boolean hasModifier(NativeKeyEvent event, int modifier) {
        return (event.getModifiers() & modifier) != 0;
    }

    private void fire(HotkeyCommand command) {
        long now = System.nanoTime();
        Long lastFireTime = lastFireTimes.get(command);
        if (lastFireTime != null && now - lastFireTime < REPEAT_GUARD_NANOS) {
            return;
        }
        lastFireTimes.put(command, now);
        commandConsumer.accept(command);
    }
}
