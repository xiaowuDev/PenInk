package com.pencli.book.domain.service;

import com.pencli.book.domain.model.AnnotationDocument;
import com.pencli.book.domain.model.BrushStyle;
import com.pencli.book.domain.model.CanvasPoint;
import com.pencli.book.domain.model.EraseChange;
import com.pencli.book.domain.model.EraserPath;
import com.pencli.book.domain.model.InkColor;
import com.pencli.book.domain.model.InkStroke;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

class EraseStrokeServiceTest {
    private final EraseStrokeService service = new EraseStrokeService();

    @Test
    void eraserSplitsStrokeIntoRemainingFragments() {
        InkStroke stroke = InkStroke.create(
                List.of(point(0, 0), point(10, 0), point(20, 0), point(30, 0), point(40, 0), point(50, 0), point(60, 0)),
                new BrushStyle(InkColor.RED, 1.0)
        );
        EraserPath eraserPath = new EraserPath(List.of(point(30, -10), point(30, 10)), 1.0);

        EraseChange change = service.erase(List.of(stroke), eraserPath);

        AnnotationDocument document = new AnnotationDocument();
        document.addStroke(stroke);
        assertTrue(document.applyErase(change));

        assertEquals(2, document.strokes().size());
        assertEquals(List.of(point(0, 0), point(10, 0)), document.strokes().get(0).points());
        assertEquals(List.of(point(50, 0), point(60, 0)), document.strokes().get(1).points());
    }

    @Test
    void eraserNoopsWhenPathDoesNotHitStroke() {
        InkStroke stroke = InkStroke.create(
                List.of(point(0, 0), point(10, 0), point(20, 0)),
                new BrushStyle(InkColor.RED, 1.0)
        );
        EraserPath eraserPath = new EraserPath(List.of(point(100, 100)), 1.0);

        EraseChange change = service.erase(List.of(stroke), eraserPath);

        assertFalse(change.hasChanges());
    }

    private static CanvasPoint point(double x, double y) {
        return new CanvasPoint(x, y, 0L);
    }
}
