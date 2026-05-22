package com.pencli.book.domain.model;

import java.util.List;
import java.util.Objects;

public record EraserPath(List<CanvasPoint> points, double radius) {
    public EraserPath {
        Objects.requireNonNull(points, "points");
        if (points.isEmpty()) {
            throw new IllegalArgumentException("eraser path must contain at least one point");
        }
        if (!Double.isFinite(radius) || radius <= 0) {
            throw new IllegalArgumentException("eraser radius must be positive and finite");
        }
        points = List.copyOf(points);
    }

    public CanvasPoint lastPoint() {
        return points.getLast();
    }
}
