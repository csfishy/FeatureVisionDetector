using FeatureVision.Core.Models;
using System.IO.Compression;
using System.Text.Json;

namespace FeatureVision.Core.IO;

public sealed class FeatureFileReader
{
    private const string FeatureJsonEntryName = "feature.json";
    private const string ExpectedFormatName = "FeatureVision.FeaturePackage";
    private const string SamplesRoot = "samples";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<FeatureFileManifest> ReadAsync(
        string packagePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        return ReadCoreAsync(packagePath, cancellationToken);
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
            var manifestEntry = archive.GetEntry(FeatureJsonEntryName);
            if (manifestEntry is null)
            {
                throw new FileNotFoundException(
                    "The feature package is missing feature.json.",
                    FeatureJsonEntryName);
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
            throw new InvalidDataException("The feature package is not a valid ZIP-based .featurepkg file.", ex);
        }
    }

    private static void ValidateManifest(FeatureFileManifest manifest, ZipArchive archive)
    {
        if (!string.Equals(manifest.FormatName, ExpectedFormatName, StringComparison.Ordinal))
        {
            throw new InvalidDataException("The feature package format name is not supported.");
        }

        if (string.IsNullOrWhiteSpace(manifest.FormatVersion))
        {
            throw new InvalidDataException("The feature package format version is missing.");
        }

        if (manifest.FeatureModel is null)
        {
            throw new InvalidDataException("The feature package is missing its feature model.");
        }

        if (manifest.FeatureModel.Samples is null)
        {
            throw new InvalidDataException("The feature package is missing its sample list.");
        }

        foreach (var sample in manifest.FeatureModel.Samples)
        {
            ValidatePackageEntryReference(archive, sample.ImagePath, "sample image");
            ValidatePackageEntryReference(archive, sample.MaskPath, "mask image");
        }
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

        if (!normalized.StartsWith($"{SamplesRoot}/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Feature package sample files must be stored under samples/.");
        }

        return normalized;
    }
}
