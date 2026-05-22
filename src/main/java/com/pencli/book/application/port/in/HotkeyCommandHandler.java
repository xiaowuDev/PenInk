package com.pencli.book.application.port.in;

@FunctionalInterface
public interface HotkeyCommandHandler {
    void handle(HotkeyCommand command);
}
