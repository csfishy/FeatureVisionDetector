namespace FeatureVision.Core.Models;

public sealed class GeometryResult
{
    public Point2D Center { get; set; } = new();

    public double CenterX
    {
        get => Center.X;
        set => Center.X = value;
    }

    public double CenterY
    {
        get => Center.Y;
        set => Center.Y = value;
    }

    public double RotationAngleDegrees { get; set; }

    public RoiRect BoundingBox { get; set; } = new();

    public double Area { get; set; }
}
