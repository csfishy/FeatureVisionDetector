namespace FeatureVision.Core.Models;

/// <summary>
/// Result for one foreground component found after thresholding an image-processing response.
/// </summary>
public sealed class ConnectedComponentResult
{
    /// <summary>
    /// One-based component number in the current result list.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Component center in image coordinates.
    /// </summary>
    public Point2D Center { get; set; } = new();

    /// <summary>
    /// Axis-aligned bounding box around the component.
    /// </summary>
    public RoiRect BoundingBox { get; set; } = new();

    /// <summary>
    /// Contour area in pixels.
    /// </summary>
    public double AreaPixels { get; set; }

    /// <summary>
    /// Component orientation in degrees, estimated from the foreground shape.
    /// </summary>
    public double RotationAngleDegrees { get; set; }

    /// <summary>
    /// Average normalized BlackHat response inside the component, from 0 to 1.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Shape similarity to the loaded reference mask, from 0 to 1.
    /// </summary>
    public double ShapeScore { get; set; }

    /// <summary>
    /// Shape comparison variant that produced the best score.
    /// </summary>
    public string ShapeTransform { get; set; } = string.Empty;

    /// <summary>
    /// Average normalized BlackHat response inside the component, from 0 to 1.
    /// </summary>
    public double ResponseScore { get; set; }

    /// <summary>
    /// Component height divided by the closest reference feature height.
    /// </summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>
    /// Height divided by width for the component bounding box.
    /// </summary>
    public double AspectRatio { get; set; }
}
