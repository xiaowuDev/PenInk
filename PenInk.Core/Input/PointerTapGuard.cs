namespace PenInk.Core.Input;

public sealed class PointerTapGuard
{
    private const double DotDistanceThreshold = 3.0;

    private readonly InkPoint start;
    private readonly DateTime startedUtc;

    private PointerTapGuard(InkPoint start)
    {
        this.start = start;
        startedUtc = DateTime.UtcNow;
    }

    public static PointerTapGuard Start(InkPoint point) => new(point);

    public PointerTap Finish(InkPoint end)
    {
        // 短触距离很小时视为点画，用于补偿数位板轻点不成线的问题。
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        return new PointerTap(startedUtc, end, distance <= DotDistanceThreshold);
    }
}
