package com.pencli.book.domain.service;

import com.pencli.book.domain.model.CanvasPoint;
import com.pencli.book.domain.model.EraseChange;
import com.pencli.book.domain.model.EraserPath;
import com.pencli.book.domain.model.InkStroke;
import com.pencli.book.domain.model.StrokeId;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;

public final class EraseStrokeService {
    public EraseChange erase(List<InkStroke> strokes, EraserPath eraserPath) {
        Objects.requireNonNull(strokes, "strokes");
        Objects.requireNonNull(eraserPath, "eraserPath");

        Map<StrokeId, List<InkStroke>> replacements = new LinkedHashMap<>();
        for (InkStroke stroke : strokes) {
            List<InkStroke> fragments = eraseStroke(stroke, eraserPath);
            if (fragments != null) {
                replacements.put(stroke.id(), fragments);
            }
        }
        return EraseChange.of(replacements);
    }

    private List<InkStroke> eraseStroke(InkStroke stroke, EraserPath eraserPath) {
        List<CanvasPoint> points = stroke.points();
        boolean[] erased = new boolean[points.size()];
        double threshold = eraserPath.radius() + stroke.style().width() / 2.0;

        if (points.size() == 1) {
            erased[0] = distancePointToPath(points.getFirst(), eraserPath.points()) <= threshold;
        } else {
            for (int i = 1; i < points.size(); i++) {
                if (distanceSegmentToPath(points.get(i - 1), points.get(i), eraserPath.points()) <= threshold) {
                    erased[i - 1] = true;
                    erased[i] = true;
                }
            }
        }

        boolean changed = false;
        for (boolean value : erased) {
            changed |= value;
        }
        if (!changed) {
            return null;
        }

        List<InkStroke> fragments = new ArrayList<>();
        List<CanvasPoint> fragmentPoints = new ArrayList<>();
        for (int i = 0; i < points.size(); i++) {
            if (erased[i]) {
                flushFragment(stroke, fragments, fragmentPoints);
            } else {
                fragmentPoints.add(points.get(i));
            }
        }
        flushFragment(stroke, fragments, fragmentPoints);
        return fragments;
    }

    private static void flushFragment(InkStroke source, List<InkStroke> fragments, List<CanvasPoint> fragmentPoints) {
        if (fragmentPoints.size() >= 2 || source.points().size() == 1 && fragmentPoints.size() == 1) {
            fragments.add(source.fragment(fragmentPoints));
        }
        fragmentPoints.clear();
    }

    private static double distancePointToPath(CanvasPoint point, List<CanvasPoint> path) {
        if (path.size() == 1) {
            return distance(point, path.getFirst());
        }

        double minimum = Double.MAX_VALUE;
        for (int i = 1; i < path.size(); i++) {
            minimum = Math.min(minimum, distancePointToSegment(point, path.get(i - 1), path.get(i)));
        }
        return minimum;
    }

    private static double distanceSegmentToPath(CanvasPoint a, CanvasPoint b, List<CanvasPoint> path) {
        if (path.size() == 1) {
            return distancePointToSegment(path.getFirst(), a, b);
        }

        double minimum = Double.MAX_VALUE;
        for (int i = 1; i < path.size(); i++) {
            minimum = Math.min(minimum, distanceSegmentToSegment(a, b, path.get(i - 1), path.get(i)));
        }
        return minimum;
    }

    private static double distanceSegmentToSegment(CanvasPoint a, CanvasPoint b, CanvasPoint c, CanvasPoint d) {
        if (segmentsIntersect(a, b, c, d)) {
            return 0.0;
        }
        double abToC = distancePointToSegment(c, a, b);
        double abToD = distancePointToSegment(d, a, b);
        double cdToA = distancePointToSegment(a, c, d);
        double cdToB = distancePointToSegment(b, c, d);
        return Math.min(Math.min(abToC, abToD), Math.min(cdToA, cdToB));
    }

    private static double distancePointToSegment(CanvasPoint point, CanvasPoint a, CanvasPoint b) {
        double dx = b.x() - a.x();
        double dy = b.y() - a.y();
        double lengthSquared = dx * dx + dy * dy;
        if (lengthSquared == 0.0) {
            return distance(point, a);
        }
        double t = ((point.x() - a.x()) * dx + (point.y() - a.y()) * dy) / lengthSquared;
        double clamped = Math.max(0.0, Math.min(1.0, t));
        double projectedX = a.x() + clamped * dx;
        double projectedY = a.y() + clamped * dy;
        return Math.hypot(point.x() - projectedX, point.y() - projectedY);
    }

    private static double distance(CanvasPoint a, CanvasPoint b) {
        return Math.hypot(a.x() - b.x(), a.y() - b.y());
    }

    private static boolean segmentsIntersect(CanvasPoint a, CanvasPoint b, CanvasPoint c, CanvasPoint d) {
        double o1 = orientation(a, b, c);
        double o2 = orientation(a, b, d);
        double o3 = orientation(c, d, a);
        double o4 = orientation(c, d, b);

        if (o1 == 0.0 && onSegment(a, c, b)) {
            return true;
        }
        if (o2 == 0.0 && onSegment(a, d, b)) {
            return true;
        }
        if (o3 == 0.0 && onSegment(c, a, d)) {
            return true;
        }
        if (o4 == 0.0 && onSegment(c, b, d)) {
            return true;
        }
        return (o1 > 0.0) != (o2 > 0.0) && (o3 > 0.0) != (o4 > 0.0);
    }

    private static double orientation(CanvasPoint a, CanvasPoint b, CanvasPoint c) {
        double value = (b.y() - a.y()) * (c.x() - b.x()) - (b.x() - a.x()) * (c.y() - b.y());
        return Math.abs(value) < 0.000001 ? 0.0 : value;
    }

    private static boolean onSegment(CanvasPoint a, CanvasPoint point, CanvasPoint b) {
        return point.x() <= Math.max(a.x(), b.x())
                && point.x() >= Math.min(a.x(), b.x())
                && point.y() <= Math.max(a.y(), b.y())
                && point.y() >= Math.min(a.y(), b.y());
    }
}
