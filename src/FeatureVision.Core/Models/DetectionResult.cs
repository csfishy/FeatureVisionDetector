namespace FeatureVision.Core.Models;

/// <summary>
/// Runtime result for one accepted feature match.
/// </summary>
public sealed class DetectionResult
{
    /// <summary>
    /// Optional sample identifier that produced the match.
    /// </summary>
    public string FeatureSampleId { get; set; } = string.Empty;

    /// <summary>
    /// Detected feature center in live-frame coordinates.
    /// </summary>
    public Point2D Center { get; set; } = new();

    /// <summary>
    /// Detected rotation angle using the model angle convention.
    /// </summary>
    public double RotationAngleDegrees { get; set; }

    /// <summary>
    /// Matching confidence or similarity score from the selected matcher.
    /// </summary>
    public double MatchingScore { get; set; }

    /// <summary>
    /// Size ratio relative to the reference feature.
    /// </summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>
    /// Shape comparison variant that produced the accepted match.
    /// </summary>
    public string ShapeTransform { get; set; } = string.Empty;

    /// <summary>
    /// Axis-aligned bounding box around the detected feature in live-frame coordinates.
    /// </summary>
    public RoiRect BoundingBox { get; set; } = new();
}
