namespace FeatureVision.Core.Models;

/// <summary>
/// Serializable axis-aligned rectangle in image coordinates.
/// </summary>
public sealed class RoiRect
{
    /// <summary>
    /// Left edge X coordinate in pixels.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Top edge Y coordinate in pixels.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Rectangle width in pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Rectangle height in pixels.
    /// </summary>
    public int Height { get; set; }
}
