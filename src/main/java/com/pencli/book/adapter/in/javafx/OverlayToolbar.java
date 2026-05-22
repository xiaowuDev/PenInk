package com.pencli.book.adapter.in.javafx;

import com.pencli.book.application.service.AnnotationSessionService;
import com.pencli.book.application.service.AnnotationSessionView;
import com.pencli.book.domain.model.InkColor;
import com.pencli.book.domain.model.ToolMode;
import javafx.geometry.Insets;
import javafx.geometry.Orientation;
import javafx.scene.Node;
import javafx.scene.control.Button;
import javafx.scene.control.ColorPicker;
import javafx.scene.control.Separator;
import javafx.scene.control.Slider;
import javafx.scene.control.Tooltip;
import javafx.scene.layout.Region;
import javafx.scene.layout.VBox;
import javafx.scene.paint.Color;

import java.util.Objects;

public final class OverlayToolbar {
    private final VBox root = new VBox(8.0);
    private final Button penButton = toolButton("P", "Pen  Ctrl+Alt+P");
    private final Button eraserButton = toolButton("E", "Eraser");
    private final Button undoButton = toolButton("Z", "Undo  Ctrl+Z");
    private final Button clearButton = toolButton("C", "Clear  Ctrl+Alt+C");
    private final Button mouseButton = toolButton("M", "Mouse passthrough  Ctrl+Alt+M");
    private final Button hideButton = toolButton("H", "Hide  Esc");
    private final ColorPicker colorPicker = new ColorPicker(Color.rgb(255, 48, 48));
    private final Slider sizeSlider = new Slider(4.0, 48.0, 6.0);

    private boolean updating;

    public OverlayToolbar() {
        root.getStyleClass().add("overlay-toolbar");
        root.setPadding(new Insets(10.0));
        root.setFillWidth(false);
        root.setPrefWidth(58.0);
        root.setMinWidth(Region.USE_PREF_SIZE);
        root.setMaxWidth(Region.USE_PREF_SIZE);
        root.setMinHeight(Region.USE_PREF_SIZE);
        root.setMaxHeight(Region.USE_PREF_SIZE);
        colorPicker.getStyleClass().add("toolbar-color-picker");
        colorPicker.setTooltip(new Tooltip("Color"));

        sizeSlider.setOrientation(Orientation.VERTICAL);
        sizeSlider.setShowTickMarks(false);
        sizeSlider.setShowTickLabels(false);
        sizeSlider.setMajorTickUnit(8.0);
        sizeSlider.setBlockIncrement(1.0);
        sizeSlider.setPrefHeight(98.0);
        sizeSlider.getStyleClass().add("toolbar-size-slider");

        root.getChildren().addAll(
                penButton,
                eraserButton,
                undoButton,
                clearButton,
                colorPicker,
                new Separator(),
                sizeSlider,
                new Separator(),
                mouseButton,
                hideButton
        );
    }

    public Node root() {
        return root;
    }

    public void bindActions(AnnotationSessionService service) {
        Objects.requireNonNull(service, "service");
        penButton.setOnAction(event -> service.activatePen());
        eraserButton.setOnAction(event -> service.activateEraser());
        undoButton.setOnAction(event -> service.undo());
        clearButton.setOnAction(event -> service.clear());
        mouseButton.setOnAction(event -> service.activateMousePassthrough());
        hideButton.setOnAction(event -> service.hideOverlay());
        colorPicker.valueProperty().addListener((observable, oldValue, newValue) -> {
            if (!updating && newValue != null) {
                service.setBrushColor(fromFxColor(newValue));
            }
        });
        sizeSlider.valueProperty().addListener((observable, oldValue, newValue) -> {
            if (updating) {
                return;
            }
            double value = newValue.doubleValue();
            if (service.mode() == ToolMode.ERASER) {
                service.setEraserRadius(value);
            } else {
                service.setBrushWidth(value);
            }
        });
    }

    public void setView(AnnotationSessionView view) {
        updating = true;
        try {
            boolean interactive = view.mode() == ToolMode.PEN || view.mode() == ToolMode.ERASER;
            root.setVisible(interactive);
            root.setManaged(interactive);
            setSelected(penButton, view.mode() == ToolMode.PEN);
            setSelected(eraserButton, view.mode() == ToolMode.ERASER);
            undoButton.setDisable(!view.canUndo());
            colorPicker.setValue(toFxColor(view.brushStyle().color()));
            sizeSlider.setValue(view.mode() == ToolMode.ERASER ? view.eraserRadius() : view.brushStyle().width());
        } finally {
            updating = false;
        }
    }

    private static Button toolButton(String text, String tooltip) {
        Button button = new Button(text);
        button.getStyleClass().add("toolbar-button");
        button.setTooltip(new Tooltip(tooltip));
        button.setFocusTraversable(false);
        return button;
    }

    private static void setSelected(Button button, boolean selected) {
        button.getStyleClass().remove("selected");
        if (selected) {
            button.getStyleClass().add("selected");
        }
    }

    private static Color toFxColor(InkColor color) {
        return Color.rgb(color.red(), color.green(), color.blue(), color.alpha() / 255.0);
    }

    private static InkColor fromFxColor(Color color) {
        return new InkColor(
                toChannel(color.getRed()),
                toChannel(color.getGreen()),
                toChannel(color.getBlue()),
                toChannel(color.getOpacity())
        );
    }

    private static int toChannel(double value) {
        return (int) Math.round(Math.max(0.0, Math.min(1.0, value)) * 255.0);
    }
}
