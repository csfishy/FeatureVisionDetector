namespace FeatureVision.AnnotationTool.Models;

internal sealed class MeasurementBox
{
    public int Id { get; set; }

    public PointF Center { get; set; }

    public float Width { get; set; }

    public float Height { get; set; }

    public float RotationDegrees { get; set; }

    public bool CenterLineFollowsLongSide { get; set; } = true;

    public float CenterLineDirectionDegrees { get; set; }

    public float SearchDirectionDegrees { get; set; }
}
