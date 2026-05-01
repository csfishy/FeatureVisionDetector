namespace FeatureVision.Core.Models;

/// <summary>
/// Represents one annotated feature sample, including its source image, mask, and measured geometry.
/// </summary>
public sealed class FeatureSample
{
    /// <summary>
    /// Stable identifier for this sample within the feature package.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Optional display name for the annotated sample.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Package-relative path to the source sample image.
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Package-relative path to the binary mask image for this sample.
    /// </summary>
    public string MaskPath { get; set; } = string.Empty;

    /// <summary>
    /// Pixel size of the source image and aligned mask.
    /// </summary>
    public Size2D ImageSize { get; set; } = new();

    /// <summary>
    /// Center of the foreground mask in source image coordinates.
    /// </summary>
    public Point2D Center { get; set; } = new();

    /// <summary>
    /// Rotation angle measured using the model angle convention.
    /// </summary>
    public double RotationAngleDegrees { get; set; }

    /// <summary>
    /// Axis-aligned bounding box around the foreground mask.
    /// </summary>
    public RoiRect BoundingBox { get; set; } = new();

    /// <summary>
    /// Number of foreground pixels in the binary mask.
    /// </summary>
    public double AreaPixels { get; set; }
}
