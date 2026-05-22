package com.pencli.book.domain.model;

import java.util.ArrayDeque;
import java.util.ArrayList;
import java.util.Deque;
import java.util.List;
import java.util.Objects;

public final class AnnotationDocument {
    private final List<InkStroke> strokes = new ArrayList<>();
    private final Deque<UndoableOperation> history = new ArrayDeque<>();

    public List<InkStroke> strokes() {
        return List.copyOf(strokes);
    }

    public boolean canUndo() {
        return !history.isEmpty();
    }

    public void addStroke(InkStroke stroke) {
        Objects.requireNonNull(stroke, "stroke");
        strokes.add(stroke);
        history.push(new RemoveStrokeOperation(stroke.id()));
    }

    public boolean clear() {
        if (strokes.isEmpty()) {
            return false;
        }
        List<InkStroke> before = strokes();
        strokes.clear();
        history.push(new RestoreSnapshotOperation(before));
        return true;
    }

    public boolean applyErase(EraseChange change) {
        Objects.requireNonNull(change, "change");
        if (!change.hasChanges()) {
            return false;
        }

        List<InkStroke> before = strokes();
        List<InkStroke> after = new ArrayList<>();
        for (InkStroke stroke : strokes) {
            if (change.replaces(stroke.id())) {
                after.addAll(change.replacementsFor(stroke.id()));
            } else {
                after.add(stroke);
            }
        }

        if (after.equals(strokes)) {
            return false;
        }

        strokes.clear();
        strokes.addAll(after);
        history.push(new RestoreSnapshotOperation(before));
        return true;
    }

    public boolean undo() {
        if (history.isEmpty()) {
            return false;
        }
        history.pop().undo(this);
        return true;
    }

    private void restore(List<InkStroke> snapshot) {
        strokes.clear();
        strokes.addAll(snapshot);
    }

    private void removeById(StrokeId strokeId) {
        strokes.removeIf(stroke -> stroke.id().equals(strokeId));
    }

    private interface UndoableOperation {
        void undo(AnnotationDocument document);
    }

    private record RemoveStrokeOperation(StrokeId strokeId) implements UndoableOperation {
        @Override
        public void undo(AnnotationDocument document) {
            document.removeById(strokeId);
        }
    }

    private record RestoreSnapshotOperation(List<InkStroke> snapshot) implements UndoableOperation {
        private RestoreSnapshotOperation {
            snapshot = List.copyOf(snapshot);
        }

        @Override
        public void undo(AnnotationDocument document) {
            document.restore(snapshot);
        }
    }
}
