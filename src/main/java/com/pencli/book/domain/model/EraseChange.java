package com.pencli.book.domain.model;

import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;

public final class EraseChange {
    private static final EraseChange NONE = new EraseChange(Map.of());

    private final Map<StrokeId, List<InkStroke>> replacements;

    private EraseChange(Map<StrokeId, List<InkStroke>> replacements) {
        this.replacements = Map.copyOf(replacements);
    }

    public static EraseChange none() {
        return NONE;
    }

    public static EraseChange of(Map<StrokeId, List<InkStroke>> replacements) {
        Objects.requireNonNull(replacements, "replacements");
        if (replacements.isEmpty()) {
            return NONE;
        }
        Map<StrokeId, List<InkStroke>> copy = new LinkedHashMap<>();
        replacements.forEach((id, strokes) -> copy.put(id, List.copyOf(strokes)));
        return new EraseChange(copy);
    }

    public boolean hasChanges() {
        return !replacements.isEmpty();
    }

    public boolean replaces(StrokeId strokeId) {
        return replacements.containsKey(strokeId);
    }

    public List<InkStroke> replacementsFor(StrokeId strokeId) {
        return replacements.getOrDefault(strokeId, List.of());
    }
}
