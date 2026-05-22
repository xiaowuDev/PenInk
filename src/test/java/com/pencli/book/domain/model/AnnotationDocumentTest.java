package com.pencli.book.domain.model;

import org.junit.jupiter.api.Test;

import java.util.List;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

class AnnotationDocumentTest {
    @Test
    void undoRemovesLastAddedStroke() {
        AnnotationDocument document = new AnnotationDocument();
        document.addStroke(stroke(0, 0, 10, 10));

        assertEquals(1, document.strokes().size());
        assertTrue(document.canUndo());

        assertTrue(document.undo());
        assertTrue(document.strokes().isEmpty());
        assertFalse(document.canUndo());
    }

    @Test
    void clearCanBeUndone() {
        AnnotationDocument document = new AnnotationDocument();
        document.addStroke(stroke(0, 0, 10, 10));
        document.addStroke(stroke(20, 20, 30, 30));

        assertTrue(document.clear());
        assertTrue(document.strokes().isEmpty());

        assertTrue(document.undo());
        assertEquals(2, document.strokes().size());
    }

    private static InkStroke stroke(double x1, double y1, double x2, double y2) {
        return InkStroke.create(
                List.of(point(x1, y1), point(x2, y2)),
                new BrushStyle(InkColor.RED, 4.0)
        );
    }

    private static CanvasPoint point(double x, double y) {
        return new CanvasPoint(x, y, 0L);
    }
}
