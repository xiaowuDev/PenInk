package com.pencli.book.application.port.out;

public interface OverlayWindowPort {
    void showInteractive();

    void showPassthrough();

    void hideOverlay();
}
