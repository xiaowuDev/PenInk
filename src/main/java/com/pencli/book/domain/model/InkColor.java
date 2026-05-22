package com.pencli.book.domain.model;

public record InkColor(int red, int green, int blue, int alpha) {
    public static final InkColor RED = new InkColor(255, 48, 48, 255);
    public static final InkColor YELLOW = new InkColor(255, 222, 0, 255);
    public static final InkColor BLUE = new InkColor(35, 120, 255, 255);
    public static final InkColor GREEN = new InkColor(30, 180, 105, 255);
    public static final InkColor WHITE = new InkColor(255, 255, 255, 255);

    public InkColor {
        requireChannel("red", red);
        requireChannel("green", green);
        requireChannel("blue", blue);
        requireChannel("alpha", alpha);
    }

    public static InkColor rgb(int red, int green, int blue) {
        return new InkColor(red, green, blue, 255);
    }

    private static void requireChannel(String name, int value) {
        if (value < 0 || value > 255) {
            throw new IllegalArgumentException(name + " must be between 0 and 255");
        }
    }
}
