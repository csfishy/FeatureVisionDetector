using System.IO.Compression;
using System.Text.Json;
using FeatureVision.Core.IO;
using FeatureVision.Core.Models;

namespace FeatureVision.Core.Tests;

public sealed class FeatureFileTests : IDisposable
{
    private readonly string testDirectory = Path.Combine(
        Path.GetTempPath(),
        "FeatureVision.Core.Tests",
        Guid.NewGuid().ToString("N"));

    public FeatureFileTests()
    {
        Directory.CreateDirectory(testDirectory);
    }

    [Fact]
    public async Task WriterAndReaderRoundTripCanonicalPackage()
    {
        var imagePath = CreateAsset("source.png", [1, 2, 3]);
        var maskPath = CreateAsset("mask.png", [0, 255, 0]);
        var packagePath = Path.Combine(testDirectory, "target.fvfeature");
        var manifest = CreateManifest(imagePath, maskPath);

        await new FeatureFileWriter().WriteAsync(packagePath, manifest);
        var actual = await new FeatureFileReader().ReadAsync(packagePath);

        Assert.Equal(FeaturePackageFormat.FormatName, actual.FormatName);
        Assert.Equal(FeaturePackageFormat.CurrentVersion, actual.FormatVersion);
        var sample = Assert.Single(actual.FeatureModel.Samples);
        Assert.Equal("samples/0001-sample-0001/image.png", sample.ImagePath);
        Assert.Equal("samples/0001-sample-0001/masks/feature.png", sample.MaskPath);

        using var archive = ZipFile.OpenRead(packagePath);
        Assert.NotNull(archive.GetEntry(FeaturePackageFormat.FeatureJsonEntryName));
        Assert.NotNull(archive.GetEntry(sample.ImagePath));
        Assert.NotNull(archive.GetEntry(sample.MaskPath));
    }

    [Fact]
    public async Task WriterRejectsLegacyFeaturepkgExtension()
    {
        var packagePath = Path.Combine(testDirectory, "target.featurepkg");
        var manifest = CreateManifest(
            CreateAsset("source.png", [1]),
            CreateAsset("mask.png", [255]));

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => new FeatureFileWriter().WriteAsync(packagePath, manifest));

        Assert.Contains(FeaturePackageFormat.FileExtension, exception.Message);
    }

    [Fact]
    public async Task ReaderRejectsParentTraversalReference()
    {
        var packagePath = Path.Combine(testDirectory, "traversal.fvfeature");
        var manifest = CreateManifest("unused.png", "unused-mask.png");
        manifest.FeatureModel.Samples[0].ImagePath = "samples/../outside.png";
        manifest.FeatureModel.Samples[0].MaskPath = "samples/0001/mask.png";
        CreatePackage(packagePath, manifest, ["samples/0001/mask.png"]);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => new FeatureFileReader().ReadAsync(packagePath));

        Assert.Contains("invalid file reference", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReaderRejectsUnsafeDetectionSettings()
    {
        var packagePath = Path.Combine(testDirectory, "unsafe-settings.fvfeature");
        var manifest = CreateManifest("unused.png", "unused-mask.png");
        manifest.FeatureModel.Samples[0].ImagePath = "samples/0001/image.png";
        manifest.FeatureModel.Samples[0].MaskPath = "samples/0001/mask.png";
        manifest.DetectionSettings.MaximumDetections = 0;
        CreatePackage(
            packagePath,
            manifest,
            ["samples/0001/image.png", "samples/0001/mask.png"]);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => new FeatureFileReader().ReadAsync(packagePath));

        Assert.Contains("unsafe detection settings", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReaderRejectsUnsupportedFormatVersion()
    {
        var packagePath = Path.Combine(testDirectory, "future.fvfeature");
        var manifest = CreateManifest("unused.png", "unused-mask.png");
        manifest.FormatVersion = "2.0";
        manifest.FeatureModel.Samples[0].ImagePath = "samples/0001/image.png";
        manifest.FeatureModel.Samples[0].MaskPath = "samples/0001/mask.png";
        CreatePackage(
            packagePath,
            manifest,
            ["samples/0001/image.png", "samples/0001/mask.png"]);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => new FeatureFileReader().ReadAsync(packagePath));

        Assert.Contains("version is not supported", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    private string CreateAsset(string name, byte[] contents)
    {
        var path = Path.Combine(testDirectory, name);
        File.WriteAllBytes(path, contents);
        return path;
    }

    private static FeatureFileManifest CreateManifest(string imagePath, string maskPath)
    {
        return new FeatureFileManifest
        {
            FeatureModel = new FeatureModel
            {
                Id = "model-0001",
                Name = "Test model",
                Samples =
                [
                    new FeatureSample
                    {
                        Id = "sample-0001",
                        Name = "Test sample",
                        ImagePath = imagePath,
                        MaskPath = maskPath,
                        ImageSize = new Size2D { Width = 32, Height = 24 },
                        Center = new Point2D { X = 15.5, Y = 11.5 },
                        BoundingBox = new RoiRect { X = 4, Y = 5, Width = 12, Height = 8 },
                        RotationAngleDegrees = 0,
                        AreaPixels = 96
                    }
                ]
            },
            DetectionSettings = new DetectionSettings()
        };
    }

    private static void CreatePackage(
        string packagePath,
        FeatureFileManifest manifest,
        IEnumerable<string> assetEntries)
    {
        using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);
        var manifestEntry = archive.CreateEntry(FeaturePackageFormat.FeatureJsonEntryName);
        using (var stream = manifestEntry.Open())
        {
            JsonSerializer.Serialize(stream, manifest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        foreach (var assetEntryName in assetEntries)
        {
            using var stream = archive.CreateEntry(assetEntryName).Open();
            stream.WriteByte(0);
        }
    }
}
