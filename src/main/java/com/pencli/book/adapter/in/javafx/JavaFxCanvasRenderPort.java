package com.pencli.book.adapter.in.javafx;

import com.pencli.book.application.port.out.CanvasRenderPort;
import com.pencli.book.application.service.AnnotationSessionView;
import com.pencli.book.domain.model.CanvasPoint;
import com.pencli.book.domain.model.EraserPath;
import com.pencli.book.domain.model.InkColor;
import com.pencli.book.domain.model.InkStroke;
import com.pencli.book.domain.model.ToolMode;
import javafx.scene.Cursor;
import javafx.scene.canvas.Canvas;
import javafx.scene.canvas.GraphicsContext;
import javafx.scene.paint.Color;
import javafx.scene.shape.StrokeLineCap;
import javafx.scene.shape.StrokeLineJoin;

import java.util.List;
import java.util.Objects;

public final class JavaFxCanvasRenderPort implements CanvasRenderPort {
    private final Canvas canvas;
    private final OverlayToolbar toolbar;

    public JavaFxCanvasRenderPort(Canvas canvas, OverlayToolbar toolbar) {
        this.canvas = Objects.requireNonNull(canvas, "canvas");
        this.toolbar = Objects.requireNonNull(toolbar, "toolbar");
    }

    @Override
    public void render(AnnotationSessionView view) {
        Objects.requireNonNull(view, "view");
        GraphicsContext graphics = canvas.getGraphicsContext2D();
        graphics.clearRect(0.0, 0.0, canvas.getWidth(), canvas.getHeight());

        for (InkStroke stroke : view.strokes()) {
            drawStroke(graphics, stroke);
        }
        if (view.draftStroke() != null) {
            drawStroke(graphics, view.draftStroke());
        }
        if (view.draftEraserPath() != null) {
            drawEraser(graphics, view.draftEraserPath());
        }

        toolbar.setView(view);
        canvas.setCursor(cursorFor(view.mode()));
    }

    private static Cursor cursorFor(ToolMode mode) {
        return switch (mode) {
            case PEN -> Cursor.CROSSHAIR;
            case ERASER -> Cursor.HAND;
            case MOUSE_PASSTHROUGH, HIDDEN -> Cursor.DEFAULT;
        };
    }

    private static void drawStroke(GraphicsContext graphics, InkStroke stroke) {
        List<CanvasPoint> points = stroke.points();
        double width = stroke.style().width();
        graphics.setStroke(toFxColor(stroke.style().color()));
        graphics.setFill(toFxColor(stroke.style().color()));
        graphics.setLineWidth(width);
        graphics.setLineCap(StrokeLineCap.ROUND);
        graphics.setLineJoin(StrokeLineJoin.ROUND);

        if (points.size() == 1) {
            CanvasPoint point = points.getFirst();
            graphics.fillOval(point.x() - width / 2.0, point.y() - width / 2.0, width, width);
            return;
        }

        graphics.beginPath();
        CanvasPoint first = points.getFirst();
        graphics.moveTo(first.x(), first.y());
        for (int i = 1; i < points.size(); i++) {
            CanvasPoint point = points.get(i);
            graphics.lineTo(point.x(), point.y());
        }
        graphics.stroke();
    }

    private static void drawEraser(GraphicsContext graphics, EraserPath eraserPath) {
        CanvasPoint point = eraserPath.lastPoint();
        double radius = eraserPath.radius();
        graphics.setStroke(Color.rgb(240, 245, 255, 0.85));
        graphics.setLineWidth(1.5);
        graphics.setLineDashes(7.0, 5.0);
        graphics.strokeOval(point.x() - radius, point.y() - radius, radius * 2.0, radius * 2.0);
        graphics.setLineDashes();
    }

    private static Color toFxColor(InkColor color) {
        return Color.rgb(color.red(), color.green(), color.blue(), color.alpha() / 255.0);
    }
}
