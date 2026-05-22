package com.pencli.book.application.service;

import com.pencli.book.application.port.out.CanvasRenderPort;
import com.pencli.book.application.port.out.OverlayWindowPort;
import com.pencli.book.domain.model.AnnotationDocument;
import com.pencli.book.domain.model.CanvasPoint;
import com.pencli.book.domain.model.ToolMode;
import com.pencli.book.domain.service.EraseStrokeService;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;

class AnnotationSessionServiceTest {
    @Test
    void startsInMousePassthroughMode() {
        FakeWindowPort windowPort = new FakeWindowPort();
        RenderCapture renderCapture = new RenderCapture();
        AnnotationSessionService service = newService(windowPort, renderCapture);

        service.startInMousePassthroughMode();

        assertEquals("passthrough", windowPort.state);
        assertEquals(ToolMode.MOUSE_PASSTHROUGH, renderCapture.view.mode());
    }

    @Test
    void penGestureAddsStrokeAndCanUndoFromPassthroughMode() {
        FakeWindowPort windowPort = new FakeWindowPort();
        RenderCapture renderCapture = new RenderCapture();
        AnnotationSessionService service = newService(windowPort, renderCapture);

        service.activatePen();
        service.beginGesture(point(0, 0));
        service.continueGesture(point(10, 10));
        service.endGesture(point(20, 20));

        assertEquals(1, service.currentView().strokes().size());
        assertNotNull(renderCapture.view);

        service.activateMousePassthrough();
        service.undo();
        assertEquals(0, service.currentView().strokes().size());
    }

    private static AnnotationSessionService newService(OverlayWindowPort windowPort, CanvasRenderPort renderPort) {
        return new AnnotationSessionService(
                new AnnotationDocument(),
                new EraseStrokeService(),
                windowPort,
                renderPort
        );
    }

    private static CanvasPoint point(double x, double y) {
        return new CanvasPoint(x, y, 0L);
    }

    private static final class FakeWindowPort implements OverlayWindowPort {
        private String state = "hidden";

        @Override
        public void showInteractive() {
            state = "interactive";
        }

        @Override
        public void showPassthrough() {
            state = "passthrough";
        }

        @Override
        public void hideOverlay() {
            state = "hidden";
        }
    }

    private static final class RenderCapture implements CanvasRenderPort {
        private AnnotationSessionView view;

        @Override
        public void render(AnnotationSessionView view) {
            this.view = view;
        }
    }
}
