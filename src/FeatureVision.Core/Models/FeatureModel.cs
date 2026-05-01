namespace FeatureVision.Core.Models;

/// <summary>
/// Describes a package-level feature definition built from one or more annotated samples.
/// </summary>
public sealed class FeatureModel
{
    /// <summary>
    /// Stable identifier for the feature model inside the package.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable feature model name shown by tools.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional operator-facing description of the target feature.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Defines how rotation angles are measured throughout the package.
    /// </summary>
    public string AngleConvention { get; set; } = "degrees-clockwise-from-positive-x";

    /// <summary>
    /// Annotated image and mask samples used to detect this feature.
    /// </summary>
    public List<FeatureSample> Samples { get; set; } = new();
}
