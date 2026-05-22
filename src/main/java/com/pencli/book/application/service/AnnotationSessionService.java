package com.pencli.book.application.service;

import com.pencli.book.application.port.in.HotkeyCommand;
import com.pencli.book.application.port.in.HotkeyCommandHandler;
import com.pencli.book.application.port.out.CanvasRenderPort;
import com.pencli.book.application.port.out.OverlayWindowPort;
import com.pencli.book.domain.model.AnnotationDocument;
import com.pencli.book.domain.model.BrushStyle;
import com.pencli.book.domain.model.CanvasPoint;
import com.pencli.book.domain.model.EraserPath;
import com.pencli.book.domain.model.InkColor;
import com.pencli.book.domain.model.InkStroke;
import com.pencli.book.domain.model.ToolMode;
import com.pencli.book.domain.service.EraseStrokeService;

import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

public final class AnnotationSessionService implements HotkeyCommandHandler {
    private static final double MIN_ERASER_RADIUS = 4.0;
    private static final double MAX_ERASER_RADIUS = 64.0;

    private final AnnotationDocument document;
    private final EraseStrokeService eraseStrokeService;
    private final OverlayWindowPort overlayWindowPort;
    private final CanvasRenderPort canvasRenderPort;

    private ToolMode mode = ToolMode.HIDDEN;
    private BrushStyle brushStyle = BrushStyle.defaultPen();
    private double eraserRadius = 16.0;
    private List<CanvasPoint> gesturePoints = List.of();

    public AnnotationSessionService(
            AnnotationDocument document,
            EraseStrokeService eraseStrokeService,
            OverlayWindowPort overlayWindowPort,
            CanvasRenderPort canvasRenderPort
    ) {
        this.document = Objects.requireNonNull(document, "document");
        this.eraseStrokeService = Objects.requireNonNull(eraseStrokeService, "eraseStrokeService");
        this.overlayWindowPort = Objects.requireNonNull(overlayWindowPort, "overlayWindowPort");
        this.canvasRenderPort = Objects.requireNonNull(canvasRenderPort, "canvasRenderPort");
    }

    public void startInMousePassthroughMode() {
        mode = ToolMode.MOUSE_PASSTHROUGH;
        gesturePoints = List.of();
        overlayWindowPort.showPassthrough();
        render();
    }

    public void activatePen() {
        mode = ToolMode.PEN;
        gesturePoints = List.of();
        overlayWindowPort.showInteractive();
        render();
    }

    public void activateEraser() {
        mode = ToolMode.ERASER;
        gesturePoints = List.of();
        overlayWindowPort.showInteractive();
        render();
    }

    public void activateMousePassthrough() {
        mode = ToolMode.MOUSE_PASSTHROUGH;
        gesturePoints = List.of();
        overlayWindowPort.showPassthrough();
        render();
    }

    public void hideOverlay() {
        mode = ToolMode.HIDDEN;
        gesturePoints = List.of();
        overlayWindowPort.hideOverlay();
        render();
    }

    public void clear() {
        document.clear();
        gesturePoints = List.of();
        render();
    }

    public void undo() {
        document.undo();
        gesturePoints = List.of();
        render();
    }

    public void setBrushColor(InkColor color) {
        brushStyle = brushStyle.withColor(color);
        render();
    }

    public void setBrushWidth(double width) {
        brushStyle = brushStyle.withWidth(width);
        render();
    }

    public void setEraserRadius(double radius) {
        if (!Double.isFinite(radius) || radius < MIN_ERASER_RADIUS || radius > MAX_ERASER_RADIUS) {
            throw new IllegalArgumentException("eraser radius must be between " + MIN_ERASER_RADIUS + " and " + MAX_ERASER_RADIUS);
        }
        eraserRadius = radius;
        render();
    }

    public void beginGesture(CanvasPoint point) {
        Objects.requireNonNull(point, "point");
        if (!acceptsGestures()) {
            return;
        }
        gesturePoints = new ArrayList<>();
        gesturePoints.add(point);
        render();
    }

    public void continueGesture(CanvasPoint point) {
        Objects.requireNonNull(point, "point");
        if (!acceptsGestures() || gesturePoints.isEmpty()) {
            return;
        }
        List<CanvasPoint> next = new ArrayList<>(gesturePoints);
        next.add(point);
        gesturePoints = next;
        render();
    }

    public void endGesture(CanvasPoint point) {
        Objects.requireNonNull(point, "point");
        if (!acceptsGestures() || gesturePoints.isEmpty()) {
            return;
        }

        List<CanvasPoint> completed = new ArrayList<>(gesturePoints);
        completed.add(point);
        gesturePoints = List.of();

        if (mode == ToolMode.PEN) {
            document.addStroke(InkStroke.create(completed, brushStyle));
        } else if (mode == ToolMode.ERASER) {
            document.applyErase(eraseStrokeService.erase(document.strokes(), new EraserPath(completed, eraserRadius)));
        }
        render();
    }

    public ToolMode mode() {
        return mode;
    }

    public AnnotationSessionView currentView() {
        return new AnnotationSessionView(
                document.strokes(),
                mode,
                brushStyle,
                eraserRadius,
                draftStroke(),
                draftEraserPath(),
                document.canUndo()
        );
    }

    @Override
    public void handle(HotkeyCommand command) {
        Objects.requireNonNull(command, "command");
        switch (command) {
            case ACTIVATE_PEN -> activatePen();
            case ACTIVATE_MOUSE_PASSTHROUGH -> activateMousePassthrough();
            case CLEAR -> clear();
            case UNDO -> undo();
            case HIDE -> hideOverlay();
        }
    }

    private boolean acceptsGestures() {
        return mode == ToolMode.PEN || mode == ToolMode.ERASER;
    }

    private InkStroke draftStroke() {
        if (mode != ToolMode.PEN || gesturePoints.isEmpty()) {
            return null;
        }
        return InkStroke.create(gesturePoints, brushStyle);
    }

    private EraserPath draftEraserPath() {
        if (mode != ToolMode.ERASER || gesturePoints.isEmpty()) {
            return null;
        }
        return new EraserPath(gesturePoints, eraserRadius);
    }

    private void render() {
        canvasRenderPort.render(currentView());
    }
}
