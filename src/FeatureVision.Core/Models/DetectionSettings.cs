namespace FeatureVision.Core.Models;

/// <summary>
/// Runtime matching settings loaded from a feature package or adjusted by the operator.
/// </summary>
public sealed class DetectionSettings
{
    private double scoreThreshold = 0.7;
    private double angleMin = -30.0;
    private double angleMax = 30.0;
    private double angleStep = 2.0;

    /// <summary>
    /// Minimum accepted template matching score.
    /// </summary>
    public double MinimumScore
    {
        get => ScoreThreshold;
        set => ScoreThreshold = value;
    }

    /// <summary>
    /// Minimum accepted template matching score.
    /// </summary>
    public double ScoreThreshold
    {
        get => scoreThreshold;
        set => scoreThreshold = value;
    }

    /// <summary>
    /// Lowest rotation offset, in degrees, to search relative to the annotated feature.
    /// </summary>
    public double AngleMin
    {
        get => angleMin;
        set => angleMin = value;
    }

    /// <summary>
    /// Highest rotation offset, in degrees, to search relative to the annotated feature.
    /// </summary>
    public double AngleMax
    {
        get => angleMax;
        set => angleMax = value;
    }

    /// <summary>
    /// Rotation search interval, in degrees.
    /// </summary>
    public double AngleStep
    {
        get => angleStep;
        set => angleStep = value;
    }

    /// <summary>
    /// Lowest rotation offset, in degrees, to search relative to the annotated feature.
    /// </summary>
    public double RotationMinDegrees
    {
        get => AngleMin;
        set => AngleMin = value;
    }

    /// <summary>
    /// Highest rotation offset, in degrees, to search relative to the annotated feature.
    /// </summary>
    public double RotationMaxDegrees
    {
        get => AngleMax;
        set => AngleMax = value;
    }

    /// <summary>
    /// Rotation search interval, in degrees.
    /// </summary>
    public double RotationStepDegrees
    {
        get => AngleStep;
        set => AngleStep = value;
    }

    /// <summary>
    /// Smallest scale factor to search.
    /// </summary>
    public double ScaleMin { get; set; } = 0.9;

    /// <summary>
    /// Largest scale factor to search.
    /// </summary>
    public double ScaleMax { get; set; } = 1.1;

    /// <summary>
    /// Scale search interval.
    /// </summary>
    public double ScaleStep { get; set; } = 0.05;

    /// <summary>
    /// Odd Gaussian blur kernel size used before dark-feature enhancement.
    /// </summary>
    public int BlurKernelSize { get; set; } = 3;

    /// <summary>
    /// Odd morphology kernel size used by BlackHat dark-feature enhancement.
    /// </summary>
    public int BlackHatKernelSize { get; set; } = 11;

    /// <summary>
    /// Odd dilation kernel size used when refining a broad annotation mask to the darker line core.
    /// </summary>
    public int MaskRefinementDilateSize { get; set; } = 3;

    /// <summary>
    /// Maximum overlap allowed when suppressing duplicate detections.
    /// </summary>
    public double NmsOverlapThreshold { get; set; } = 0.35;

    /// <summary>
    /// Maximum number of accepted detections returned for one frame.
    /// </summary>
    public int MaximumDetections { get; set; } = 100;

    /// <summary>
    /// Binary threshold applied to the BlackHat response before contour extraction.
    /// </summary>
    public double ComponentThreshold { get; set; } = 40.0;

    /// <summary>
    /// Odd morphology-open kernel size used to remove small threshold noise.
    /// </summary>
    public int ComponentOpenKernelSize { get; set; } = 1;

    /// <summary>
    /// Odd morphology-close kernel size used to reconnect broken thin features.
    /// </summary>
    public int ComponentCloseKernelSize { get; set; } = 3;

    /// <summary>
    /// Minimum contour area accepted as a foreground component.
    /// </summary>
    public double ComponentMinArea { get; set; } = 20.0;

    /// <summary>
    /// Maximum contour area accepted as a foreground component.
    /// </summary>
    public double ComponentMaxArea { get; set; } = 100000.0;

    /// <summary>
    /// Minimum component bounding-box width in pixels.
    /// </summary>
    public int ComponentMinWidth { get; set; } = 1;

    /// <summary>
    /// Maximum component bounding-box width in pixels.
    /// </summary>
    public int ComponentMaxWidth { get; set; } = 10000;

    /// <summary>
    /// Minimum component bounding-box height in pixels.
    /// </summary>
    public int ComponentMinHeight { get; set; } = 5;

    /// <summary>
    /// Maximum component bounding-box height in pixels.
    /// </summary>
    public int ComponentMaxHeight { get; set; } = 10000;

    /// <summary>
    /// Minimum component height divided by width.
    /// </summary>
    public double ComponentMinAspectRatio { get; set; } = 1.5;

    /// <summary>
    /// Maximum component height divided by width.
    /// </summary>
    public double ComponentMaxAspectRatio { get; set; } = 1000.0;

    /// <summary>
    /// Weight of shape similarity in the final component score when reference masks are available.
    /// </summary>
    public double ComponentShapeScoreWeight { get; set; } = 0.95;

    /// <summary>
    /// Number of vertical samples used when comparing normalized component centerlines.
    /// </summary>
    public int ComponentShapeProfileSamples { get; set; } = 64;

    /// <summary>
    /// Exponential sensitivity for converting centerline distance into shape similarity.
    /// </summary>
    public double ComponentShapeDistanceSensitivity { get; set; } = 16.0;

    /// <summary>
    /// Align shape profiles by their principal axis before comparing, allowing rotated targets to match.
    /// </summary>
    public bool ComponentShapeNormalizeRotation { get; set; } = true;

    /// <summary>
    /// Compare mirrored shape-profile variants in addition to the original profile.
    /// </summary>
    public bool ComponentShapeAllowFlip { get; set; } = true;

    /// <summary>
    /// Optional detection region in live-frame coordinates.
    /// </summary>
    public RoiRect? RegionOfInterest { get; set; }
}
