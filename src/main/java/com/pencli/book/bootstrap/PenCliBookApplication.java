package com.pencli.book.bootstrap;

import com.pencli.book.adapter.in.hotkey.GlobalHotkeyAdapter;
import com.pencli.book.adapter.in.javafx.JavaFxCanvasRenderPort;
import com.pencli.book.adapter.in.javafx.OverlayToolbar;
import com.pencli.book.application.service.AnnotationSessionService;
import com.pencli.book.domain.model.AnnotationDocument;
import com.pencli.book.domain.model.CanvasPoint;
import com.pencli.book.domain.service.EraseStrokeService;
import com.pencli.book.infrastructure.windows.WindowsOverlayWindowAdapter;
import javafx.application.Application;
import javafx.application.Platform;
import javafx.geometry.Insets;
import javafx.geometry.Pos;
import javafx.geometry.Rectangle2D;
import javafx.scene.Scene;
import javafx.scene.canvas.Canvas;
import javafx.scene.input.KeyCode;
import javafx.scene.input.MouseButton;
import javafx.scene.layout.Pane;
import javafx.scene.layout.StackPane;
import javafx.scene.paint.Color;
import javafx.stage.Screen;
import javafx.stage.Stage;
import javafx.stage.StageStyle;

import java.util.UUID;

public final class PenCliBookApplication extends Application {
    private GlobalHotkeyAdapter hotkeyAdapter;

    public static void main(String[] args) {
        launch(args);
    }

    @Override
    public void start(Stage stage) {
        String windowTitle = "PenCliBook Overlay " + UUID.randomUUID();
        Rectangle2D bounds = Screen.getPrimary().getBounds();

        stage.initStyle(StageStyle.TRANSPARENT);
        stage.setTitle(windowTitle);
        stage.setAlwaysOnTop(true);
        stage.setX(bounds.getMinX());
        stage.setY(bounds.getMinY());
        stage.setWidth(bounds.getWidth());
        stage.setHeight(bounds.getHeight());

        Canvas canvas = new Canvas(bounds.getWidth(), bounds.getHeight());
        Pane inputSurface = new Pane();
        inputSurface.getStyleClass().add("input-surface");
        inputSurface.setPickOnBounds(true);
        OverlayToolbar toolbar = new OverlayToolbar();
        StackPane root = new StackPane(canvas, inputSurface, toolbar.root());
        root.getStyleClass().add("overlay-root");
        root.setPickOnBounds(true);
        StackPane.setAlignment(toolbar.root(), Pos.CENTER_RIGHT);
        StackPane.setMargin(toolbar.root(), new Insets(0.0, 24.0, 0.0, 0.0));

        canvas.widthProperty().bind(root.widthProperty());
        canvas.heightProperty().bind(root.heightProperty());
        inputSurface.prefWidthProperty().bind(root.widthProperty());
        inputSurface.prefHeightProperty().bind(root.heightProperty());

        Scene scene = new Scene(root, bounds.getWidth(), bounds.getHeight(), Color.TRANSPARENT);
        scene.setFill(Color.TRANSPARENT);
        scene.getStylesheets().add(getClass().getResource("/com/pencli/book/styles/overlay.css").toExternalForm());
        stage.setScene(scene);

        AnnotationSessionService service = new AnnotationSessionService(
                new AnnotationDocument(),
                new EraseStrokeService(),
                new WindowsOverlayWindowAdapter(stage, windowTitle),
                new JavaFxCanvasRenderPort(canvas, toolbar)
        );
        toolbar.bindActions(service);
        installPointerInput(inputSurface, service);
        installLocalKeys(scene, service);

        hotkeyAdapter = new GlobalHotkeyAdapter(command -> Platform.runLater(() -> service.handle(command)));
        hotkeyAdapter.start();

        stage.show();
        service.startInMousePassthroughMode();
    }

    @Override
    public void stop() {
        if (hotkeyAdapter != null) {
            hotkeyAdapter.stop();
        }
    }

    private static void installPointerInput(Pane inputSurface, AnnotationSessionService service) {
        inputSurface.setOnMousePressed(event -> {
            if (event.getButton() == MouseButton.PRIMARY) {
                service.beginGesture(CanvasPoint.now(event.getX(), event.getY()));
                event.consume();
            }
        });
        inputSurface.setOnMouseDragged(event -> {
            if (event.isPrimaryButtonDown()) {
                service.continueGesture(CanvasPoint.now(event.getX(), event.getY()));
                event.consume();
            }
        });
        inputSurface.setOnMouseReleased(event -> {
            if (event.getButton() == MouseButton.PRIMARY) {
                service.endGesture(CanvasPoint.now(event.getX(), event.getY()));
                event.consume();
            }
        });
    }

    private static void installLocalKeys(Scene scene, AnnotationSessionService service) {
        scene.setOnKeyPressed(event -> {
            if (event.getCode() == KeyCode.ESCAPE) {
                service.hideOverlay();
                event.consume();
            } else if (event.isControlDown() && event.getCode() == KeyCode.Z) {
                service.undo();
                event.consume();
            }
        });
    }
}
