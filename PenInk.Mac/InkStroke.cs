using Avalonia;
using Avalonia.Media;

namespace PenInk.Mac;

internal sealed record InkStroke(IReadOnlyList<InkSample> Samples, Color Color, double Width);

internal readonly record struct InkSample(Point Point, double Pressure);
