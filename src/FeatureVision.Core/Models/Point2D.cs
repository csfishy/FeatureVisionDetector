namespace FeatureVision.Core.Models;

/// <summary>
/// Serializable two-dimensional point in image coordinates.
/// </summary>
public sealed class Point2D
{
    /// <summary>
    /// Horizontal coordinate. In image coordinates, X increases to the right.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Vertical coordinate. In image coordinates, Y increases downward.
    /// </summary>
    public double Y { get; set; }
}
