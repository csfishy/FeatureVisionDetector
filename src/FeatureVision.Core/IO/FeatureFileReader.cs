using FeatureVision.Core.Models;
using System.IO.Compression;
using System.Text.Json;

namespace FeatureVision.Core.IO;

public sealed class FeatureFileReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<FeatureFileManifest> ReadAsync(
        string packagePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        FeaturePackageFormat.ValidateFileExtension(packagePath);

        return ReadCoreAsync(packagePath, cancellationToken);
    }

    public async Task<FeatureFileManifest> ReadAndExtractAssetsAsync(
        string packagePath,
        string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);
        FeaturePackageFormat.ValidateFileExtension(packagePath);

        var manifest = await ReadCoreAsync(packagePath, cancellationToken)
            .ConfigureAwait(false);

        Directory.CreateDirectory(destinationDirectory);

        using var archive = OpenPackage(packagePath);
        foreach (var sample in manifest.FeatureModel.Samples)
        {
            sample.ImagePath = await ExtractEntryAsync(
                    archive,
                    sample.ImagePath,
                    destinationDirectory,
                    cancellationToken)
                .ConfigureAwait(false);

            sample.MaskPath = await ExtractEntryAsync(
                    archive,
                    sample.MaskPath,
                    destinationDirectory,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return manifest;
    }

    private static async Task<FeatureFileManifest> ReadCoreAsync(
        string packagePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException("The feature package file could not be found.", packagePath);
        }

        try
        {
            using var archive = OpenPackage(packagePath);
            ValidateArchiveLimits(archive);
            var manifestEntry = archive.GetEntry(FeaturePackageFormat.FeatureJsonEntryName);
            if (manifestEntry is null)
            {
                throw new FileNotFoundException(
                    "The feature package is missing feature.json.",
                    FeaturePackageFormat.FeatureJsonEntryName);
            }

            if (manifestEntry.Length > FeaturePackageFormat.MaximumManifestBytes)
            {
                throw new InvalidDataException("feature.json exceeds the supported size limit.");
            }

            await using var manifestStream = manifestEntry.Open();
            var manifest = await JsonSerializer.DeserializeAsync<FeatureFileManifest>(
                    manifestStream,
                    JsonOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            if (manifest is null)
            {
                throw new InvalidDataException("feature.json does not contain a valid feature manifest.");
            }

            ValidateManifest(manifest, archive);

            return manifest;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("feature.json is not valid JSON.", ex);
        }
        catch (InvalidDataException)
        {
            throw;
        }
    }

    private static ZipArchive OpenPackage(string packagePath)
    {
        try
        {
            return ZipFile.OpenRead(packagePath);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException(
                $"The feature package is not a valid ZIP-based {FeaturePackageFormat.FileExtension} file.",
                ex);
        }
    }

    private static void ValidateManifest(FeatureFileManifest manifest, ZipArchive archive)
    {
        if (!string.Equals(manifest.FormatName, FeaturePackageFormat.FormatName, StringComparison.Ordinal))
        {
            throw new InvalidDataException("The feature package format name is not supported.");
        }

        if (!string.Equals(
                manifest.FormatVersion,
                FeaturePackageFormat.CurrentVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException("The feature package format version is not supported.");
        }

        if (manifest.FeatureModel is null)
        {
            throw new InvalidDataException("The feature package is missing its feature model.");
        }

        if (manifest.FeatureModel.Samples is null)
        {
            throw new InvalidDataException("The feature package is missing its sample list.");
        }

        if (manifest.FeatureModel.Samples.Count > FeaturePackageFormat.MaximumSampleCount)
        {
            throw new InvalidDataException(
                $"The feature package exceeds the {FeaturePackageFormat.MaximumSampleCount}-sample limit.");
        }

        foreach (var sample in manifest.FeatureModel.Samples)
        {
            ValidatePackageEntryReference(archive, sample.ImagePath, "sample image");
            ValidatePackageEntryReference(archive, sample.MaskPath, "mask image");
            ValidateSampleGeometry(sample);
        }

        ValidateDetectionSettings(manifest.DetectionSettings);
    }

    private static void ValidatePackageEntryReference(
        ZipArchive archive,
        string entryName,
        string description)
    {
        if (string.IsNullOrWhiteSpace(entryName))
        {
            throw new InvalidDataException($"The feature package has a missing {description} reference.");
        }

        var normalizedEntryName = NormalizeEntryName(entryName);
        if (archive.GetEntry(normalizedEntryName) is null)
        {
            throw new FileNotFoundException(
                $"The feature package is missing a referenced {description}.",
                normalizedEntryName);
        }
    }

    private static string NormalizeEntryName(string entryName)
    {
        if (Path.IsPathFullyQualified(entryName))
        {
            throw new InvalidDataException("Feature package file references must be relative paths.");
        }

        var normalized = entryName.Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrWhiteSpace(normalized) ||
            normalized.Contains("../", StringComparison.Ordinal) ||
            normalized.Equals("..", StringComparison.Ordinal))
        {
            throw new InvalidDataException("The feature package contains an invalid file reference.");
        }

        if (!normalized.StartsWith($"{FeaturePackageFormat.SamplesRoot}/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Feature package sample files must be stored under samples/.");
        }

        return normalized;
    }

    private static async Task<string> ExtractEntryAsync(
        ZipArchive archive,
        string entryName,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        var normalizedEntryName = NormalizeEntryName(entryName);
        var entry = archive.GetEntry(normalizedEntryName)
            ?? throw new FileNotFoundException(
                "The feature package is missing a referenced file.",
                normalizedEntryName);

        var destinationRoot = Path.GetFullPath(destinationDirectory);
        var destinationPath = Path.GetFullPath(Path.Combine(
            destinationRoot,
            normalizedEntryName.Replace('/', Path.DirectorySeparatorChar)));
        var requiredPrefix = destinationRoot.TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!destinationPath.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The feature package entry escapes the extraction directory.");
        }
        var destinationPathDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(destinationPathDirectory))
        {
            Directory.CreateDirectory(destinationPathDirectory);
        }

        await using var entryStream = entry.Open();
        await using var destinationStream = File.Create(destinationPath);
        await entryStream.CopyToAsync(destinationStream, cancellationToken)
            .ConfigureAwait(false);

        return destinationPath;
    }

    private static void ValidateArchiveLimits(ZipArchive archive)
    {
        if (archive.Entries.Count > FeaturePackageFormat.MaximumArchiveEntryCount)
        {
            throw new InvalidDataException("The feature package contains too many archive entries.");
        }

        long totalLength = 0;
        foreach (var entry in archive.Entries)
        {
            if (entry.Length > FeaturePackageFormat.MaximumAssetBytes &&
                !string.Equals(
                    entry.FullName,
                    FeaturePackageFormat.FeatureJsonEntryName,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException("A feature package asset exceeds the supported size limit.");
            }

            try
            {
                totalLength = checked(totalLength + entry.Length);
            }
            catch (OverflowException ex)
            {
                throw new InvalidDataException("The feature package size is invalid.", ex);
            }

            if (totalLength > FeaturePackageFormat.MaximumTotalUncompressedBytes)
            {
                throw new InvalidDataException("The feature package exceeds the uncompressed size limit.");
            }
        }
    }

    private static void ValidateSampleGeometry(FeatureSample sample)
    {
        if (sample.ImageSize is null ||
            sample.ImageSize.Width <= 0 ||
            sample.ImageSize.Height <= 0)
        {
            throw new InvalidDataException("A feature sample has invalid image dimensions.");
        }

        if (sample.Center is null ||
            !double.IsFinite(sample.Center.X) ||
            !double.IsFinite(sample.Center.Y) ||
            !double.IsFinite(sample.RotationAngleDegrees) ||
            !double.IsFinite(sample.AreaPixels) ||
            sample.AreaPixels < 0)
        {
            throw new InvalidDataException("A feature sample has invalid numeric geometry.");
        }

        if (sample.BoundingBox is null ||
            sample.BoundingBox.X < 0 ||
            sample.BoundingBox.Y < 0 ||
            sample.BoundingBox.Width < 0 ||
            sample.BoundingBox.Height < 0)
        {
            throw new InvalidDataException("A feature sample has an invalid bounding box.");
        }
    }

    private static void ValidateDetectionSettings(DetectionSettings? settings)
    {
        if (settings is null ||
            !double.IsFinite(settings.ScoreThreshold) ||
            settings.ScoreThreshold is < 0 or > 1 ||
            !double.IsFinite(settings.AngleMin) ||
            !double.IsFinite(settings.AngleMax) ||
            !double.IsFinite(settings.AngleStep) ||
            settings.AngleStep <= 0 ||
            !double.IsFinite(settings.ScaleMin) ||
            !double.IsFinite(settings.ScaleMax) ||
            !double.IsFinite(settings.ScaleStep) ||
            settings.ScaleMin <= 0 ||
            settings.ScaleMax < settings.ScaleMin ||
            settings.ScaleStep <= 0 ||
            !double.IsFinite(settings.NmsOverlapThreshold) ||
            settings.NmsOverlapThreshold is < 0 or > 1 ||
            settings.MaximumDetections is < 1 or > 10000 ||
            settings.BlurKernelSize is < 1 or > 101 ||
            settings.BlackHatKernelSize is < 3 or > 501)
        {
            throw new InvalidDataException("The feature package contains unsafe detection settings.");
        }
    }
}
