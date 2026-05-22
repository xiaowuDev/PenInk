package com.pencli.book.domain.model;

import java.util.Objects;

public record BrushStyle(InkColor color, double width) {
    public static final double MIN_WIDTH = 1.0;
    public static final double MAX_WIDTH = 48.0;

    public BrushStyle {
        Objects.requireNonNull(color, "color");
        if (!Double.isFinite(width) || width < MIN_WIDTH || width > MAX_WIDTH) {
            throw new IllegalArgumentException("width must be finite and between " + MIN_WIDTH + " and " + MAX_WIDTH);
        }
    }

    public static BrushStyle defaultPen() {
        return new BrushStyle(InkColor.RED, 6.0);
    }

    public BrushStyle withColor(InkColor nextColor) {
        return new BrushStyle(nextColor, width);
    }

    public BrushStyle withWidth(double nextWidth) {
        return new BrushStyle(color, nextWidth);
    }
}
