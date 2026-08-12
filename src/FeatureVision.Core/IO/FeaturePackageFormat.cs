namespace FeatureVision.Core.IO;

/// <summary>
/// Canonical identifiers and limits for FeatureVision packages.
/// </summary>
public static class FeaturePackageFormat
{
    public const string FormatName = "FeatureVision.FeaturePackage";
    public const string CurrentVersion = "1.0";
    public const string FileExtension = ".fvfeature";
    public const string FeatureJsonEntryName = "feature.json";
    public const string SamplesRoot = "samples";

    // Packages are user-controlled input. These limits bound archive expansion before
    // image data is handed to System.Drawing or native OpenCV decoders.
    public const int MaximumSampleCount = 256;
    public const int MaximumArchiveEntryCount = 1 + MaximumSampleCount * 2;
    public const long MaximumManifestBytes = 1 * 1024 * 1024;
    public const long MaximumAssetBytes = 128L * 1024 * 1024;
    public const long MaximumTotalUncompressedBytes = 512L * 1024 * 1024;

    public static void ValidateFileExtension(string packagePath)
    {
        if (!string.Equals(
                Path.GetExtension(packagePath),
                FileExtension,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Feature packages must use the {FileExtension} file extension.");
        }
    }
}
