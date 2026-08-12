using FeatureVision.Core.Models;
using System.IO.Compression;
using System.Text.Json;

namespace FeatureVision.Core.IO;

public sealed class FeatureFileWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public Task WriteAsync(
        string packagePath,
        FeatureFileManifest manifest,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        ArgumentNullException.ThrowIfNull(manifest);
        FeaturePackageFormat.ValidateFileExtension(packagePath);
        ValidateSourceManifest(manifest);

        return WriteCoreAsync(packagePath, manifest, cancellationToken);
    }

    private static void ValidateSourceManifest(FeatureFileManifest manifest)
    {
        if (manifest.FeatureModel is null)
        {
            throw new InvalidDataException("The feature manifest is missing its feature model.");
        }

        if (manifest.FeatureModel.Samples is null)
        {
            throw new InvalidDataException("The feature manifest is missing its sample list.");
        }

        if (manifest.FeatureModel.Samples.Count > FeaturePackageFormat.MaximumSampleCount)
        {
            throw new InvalidDataException(
                $"A feature package may contain at most {FeaturePackageFormat.MaximumSampleCount} samples.");
        }
    }

    private static async Task WriteCoreAsync(
        string packagePath,
        FeatureFileManifest manifest,
        CancellationToken cancellationToken)
    {
        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(packagePath));
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var packageManifest = CloneManifest(manifest);
        UpdatePackageEntryPaths(manifest, packageManifest);

        if (File.Exists(packagePath))
        {
            File.Delete(packagePath);
        }

        try
        {
            using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);

            await WriteManifestAsync(archive, packageManifest, cancellationToken)
                .ConfigureAwait(false);

            await WriteSampleFilesAsync(archive, manifest, packageManifest, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw new InvalidDataException("Unable to write the feature package.", ex);
        }
    }

    private static FeatureFileManifest CloneManifest(FeatureFileManifest manifest)
    {
        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        return JsonSerializer.Deserialize<FeatureFileManifest>(json, JsonOptions)
            ?? throw new InvalidDataException("The feature manifest could not be serialized.");
    }

    private static void UpdatePackageEntryPaths(
        FeatureFileManifest sourceManifest,
        FeatureFileManifest packageManifest)
    {
        var sourceSamples = sourceManifest.FeatureModel.Samples;
        var packageSamples = packageManifest.FeatureModel.Samples;

        if (packageSamples is null)
        {
            throw new InvalidDataException("The feature manifest sample list is invalid.");
        }

        if (sourceSamples.Count != packageSamples.Count)
        {
            throw new InvalidDataException("The feature manifest sample list is invalid.");
        }

        for (var index = 0; index < sourceSamples.Count; index++)
        {
            var sourceSample = sourceSamples[index];
            var packageSample = packageSamples[index];
            var sampleDirectory = GetSampleDirectory(sourceSample, index);

            packageSample.ImagePath = BuildPackageEntryName(
                sampleDirectory,
                "image",
                sourceSample.ImagePath,
                ".png");

            packageSample.MaskPath = BuildPackageEntryName(
                sampleDirectory,
                "masks/feature",
                sourceSample.MaskPath,
                ".png");
        }
    }

    private static async Task WriteManifestAsync(
        ZipArchive archive,
        FeatureFileManifest manifest,
        CancellationToken cancellationToken)
    {
        var manifestEntry = archive.CreateEntry(FeaturePackageFormat.FeatureJsonEntryName, CompressionLevel.Optimal);
        await using var manifestStream = manifestEntry.Open();
        await JsonSerializer.SerializeAsync(manifestStream, manifest, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task WriteSampleFilesAsync(
        ZipArchive archive,
        FeatureFileManifest sourceManifest,
        FeatureFileManifest packageManifest,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < sourceManifest.FeatureModel.Samples.Count; index++)
        {
            var sourceSample = sourceManifest.FeatureModel.Samples[index];
            var packageSample = packageManifest.FeatureModel.Samples[index];

            await WriteFileEntryAsync(
                    archive,
                    sourceSample.ImagePath,
                    packageSample.ImagePath,
                    cancellationToken)
                .ConfigureAwait(false);

            await WriteFileEntryAsync(
                    archive,
                    sourceSample.MaskPath,
                    packageSample.MaskPath,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async Task WriteFileEntryAsync(
        ZipArchive archive,
        string sourcePath,
        string entryName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new InvalidDataException("A sample image or mask path is missing.");
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("A sample image or mask file could not be found.", sourcePath);
        }

        var normalizedEntryName = NormalizeEntryName(entryName);
        var entry = archive.CreateEntry(normalizedEntryName, CompressionLevel.Optimal);

        await using var sourceStream = File.OpenRead(sourcePath);
        await using var entryStream = entry.Open();
        await sourceStream.CopyToAsync(entryStream, cancellationToken).ConfigureAwait(false);
    }

    private static string GetSampleDirectory(FeatureSample sample, int index)
    {
        var sampleId = string.IsNullOrWhiteSpace(sample.Id)
            ? $"sample-{index + 1:0000}"
            : SanitizePathSegment(sample.Id);

        return $"{FeaturePackageFormat.SamplesRoot}/{index + 1:0000}-{sampleId}";
    }

    private static string BuildPackageEntryName(
        string sampleDirectory,
        string baseName,
        string sourcePath,
        string defaultExtension)
    {
        var extension = Path.GetExtension(sourcePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = defaultExtension;
        }

        return $"{sampleDirectory}/{baseName}{extension.ToLowerInvariant()}";
    }

    private static string SanitizePathSegment(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value
            .Select(character => invalidCharacters.Contains(character) ? '-' : character)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "sample" : sanitized;
    }

    private static string NormalizeEntryName(string entryName)
    {
        var normalized = entryName.Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrWhiteSpace(normalized) ||
            normalized.Contains("../", StringComparison.Ordinal) ||
            normalized.Equals("..", StringComparison.Ordinal))
        {
            throw new InvalidDataException("The package contains an invalid sample entry path.");
        }

        if (!normalized.StartsWith($"{FeaturePackageFormat.SamplesRoot}/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Sample files must be stored under the samples/ package directory.");
        }

        return normalized;
    }
}
