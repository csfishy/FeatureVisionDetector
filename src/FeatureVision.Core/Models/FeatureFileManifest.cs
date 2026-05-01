namespace FeatureVision.Core.Models;

/// <summary>
/// Root JSON manifest stored as feature.json inside a feature package.
/// </summary>
public sealed class FeatureFileManifest
{
    /// <summary>
    /// Identifies the package type for validation.
    /// </summary>
    public string FormatName { get; set; } = "FeatureVision.FeaturePackage";

    /// <summary>
    /// Semantic package format version used for compatibility checks.
    /// </summary>
    public string FormatVersion { get; set; } = "1.0";

    /// <summary>
    /// UTC timestamp for when the package was created.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Name of the tool or process that created the package.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Feature definition and annotated samples contained by the package.
    /// </summary>
    public FeatureModel FeatureModel { get; set; } = new();

    /// <summary>
    /// Default runtime settings saved with the package.
    /// </summary>
    public DetectionSettings DetectionSettings { get; set; } = new();
}
