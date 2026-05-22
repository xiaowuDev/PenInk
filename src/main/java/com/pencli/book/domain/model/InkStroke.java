package com.pencli.book.domain.model;

import java.util.List;
import java.util.Objects;

public record InkStroke(StrokeId id, List<CanvasPoint> points, BrushStyle style) {
    public InkStroke {
        Objects.requireNonNull(id, "id");
        Objects.requireNonNull(points, "points");
        Objects.requireNonNull(style, "style");
        if (points.isEmpty()) {
            throw new IllegalArgumentException("stroke must contain at least one point");
        }
        points = List.copyOf(points);
    }

    public static InkStroke create(List<CanvasPoint> points, BrushStyle style) {
        return new InkStroke(StrokeId.newId(), points, style);
    }

    public InkStroke fragment(List<CanvasPoint> fragmentPoints) {
        return new InkStroke(StrokeId.newId(), fragmentPoints, style);
    }
}
