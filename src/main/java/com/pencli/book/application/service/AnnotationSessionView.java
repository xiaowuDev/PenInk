package com.pencli.book.application.service;

import com.pencli.book.domain.model.BrushStyle;
import com.pencli.book.domain.model.EraserPath;
import com.pencli.book.domain.model.InkStroke;
import com.pencli.book.domain.model.ToolMode;

import java.util.List;
import java.util.Objects;

public record AnnotationSessionView(
        List<InkStroke> strokes,
        ToolMode mode,
        BrushStyle brushStyle,
        double eraserRadius,
        InkStroke draftStroke,
        EraserPath draftEraserPath,
        boolean canUndo
) {
    public AnnotationSessionView {
        Objects.requireNonNull(strokes, "strokes");
        Objects.requireNonNull(mode, "mode");
        Objects.requireNonNull(brushStyle, "brushStyle");
        if (!Double.isFinite(eraserRadius) || eraserRadius <= 0) {
            throw new IllegalArgumentException("eraserRadius must be positive and finite");
        }
        strokes = List.copyOf(strokes);
    }
}
