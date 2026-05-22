package com.pencli.book.application.port.out;

public interface HotkeyPort extends AutoCloseable {
    void start();

    void stop();

    @Override
    default void close() {
        stop();
    }
}
