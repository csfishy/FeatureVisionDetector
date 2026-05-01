namespace FeatureVision.Core.Models;

/// <summary>
/// Serializable image or mask size in pixels.
/// </summary>
public sealed class Size2D
{
    /// <summary>
    /// Width in pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Height in pixels.
    /// </summary>
    public int Height { get; set; }
}
