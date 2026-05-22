package com.pencli.book.domain.model;

import java.util.Objects;
import java.util.UUID;

public record StrokeId(UUID value) {
    public StrokeId {
        Objects.requireNonNull(value, "value");
    }

    public static StrokeId newId() {
        return new StrokeId(UUID.randomUUID());
    }
}
