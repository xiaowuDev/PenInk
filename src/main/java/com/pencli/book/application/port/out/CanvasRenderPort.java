package com.pencli.book.application.port.out;

import com.pencli.book.application.service.AnnotationSessionView;

@FunctionalInterface
public interface CanvasRenderPort {
    void render(AnnotationSessionView view);
}
