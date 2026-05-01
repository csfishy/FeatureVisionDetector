namespace FeatureVision.Core.Models;

/// <summary>
/// Backward-compatible annotation metadata for older stubs; new package samples use <see cref="FeatureSample"/>.
/// </summary>
public sealed class FeatureAnnotation
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string MaskPath { get; set; } = string.Empty;

    public double CenterX { get; set; }

    public double CenterY { get; set; }

    public double RotationAngleDegrees { get; set; }

    public RoiRect BoundingBox { get; set; } = new();

    public double Area { get; set; }
}
