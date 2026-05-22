package com.pencli.book.domain.model;

public record CanvasPoint(double x, double y, long timestampNanos) {
    public CanvasPoint {
        if (!Double.isFinite(x) || !Double.isFinite(y)) {
            throw new IllegalArgumentException("point coordinates must be finite");
        }
    }

    public static CanvasPoint now(double x, double y) {
        return new CanvasPoint(x, y, System.nanoTime());
    }
}
