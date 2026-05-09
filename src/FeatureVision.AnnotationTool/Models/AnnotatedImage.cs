using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using FeatureVision.Core.Models;

namespace FeatureVision.AnnotationTool.Models;

internal sealed class AnnotatedImage : IDisposable
{
    public AnnotatedImage(string imagePath)
        : this(imagePath, maskPath: null, displayName: null)
    {
    }

    public AnnotatedImage(
        string imagePath,
        string? maskPath,
        string? displayName = null)
    {
        ImagePath = imagePath;
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? Path.GetFileName(imagePath)
            : displayName;
        Image = new Bitmap(imagePath);

        try
        {
            if (string.IsNullOrWhiteSpace(maskPath))
            {
                Mask = CreateMask(Image.Size);
                Overlay = CreateOverlay(Image.Size);
            }
            else
            {
                (Mask, Overlay) = CreateMaskAndOverlay(maskPath, Image.Size);
            }
        }
        catch
        {
            Image.Dispose();
            throw;
        }
    }

    public string ImagePath { get; }

    public string DisplayName { get; }

    public Bitmap Image { get; }

    public Bitmap Mask { get; }

    public Bitmap Overlay { get; }

    public List<MeasurementBox> MeasurementBoxes { get; } = new();

    public GeometryResult Geometry { get; set; } = new();

    public override string ToString()
    {
        return DisplayName;
    }

    public void SaveMask(string maskPath)
    {
        Mask.Save(maskPath, ImageFormat.Png);
    }

    public void Dispose()
    {
        Image.Dispose();
        Mask.Dispose();
        Overlay.Dispose();
    }

    private static Bitmap CreateMask(Size size)
    {
        var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Black);
        return bitmap;
    }

    private static Bitmap CreateOverlay(Size size)
    {
        var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        return bitmap;
    }

    private static (Bitmap Mask, Bitmap Overlay) CreateMaskAndOverlay(
        string maskPath,
        Size expectedSize)
    {
        using var source = new Bitmap(maskPath);
        if (source.Size != expectedSize)
        {
            throw new InvalidDataException("The package mask dimensions do not match the sample image.");
        }

        using var normalizedSource = new Bitmap(expectedSize.Width, expectedSize.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(normalizedSource))
        {
            graphics.DrawImage(source, new Rectangle(Point.Empty, expectedSize));
        }

        var mask = CreateMask(expectedSize);
        var overlay = CreateOverlay(expectedSize);

        try
        {
            ApplyLoadedMask(normalizedSource, mask, overlay);
            return (mask, overlay);
        }
        catch
        {
            mask.Dispose();
            overlay.Dispose();
            throw;
        }
    }

    private static void ApplyLoadedMask(Bitmap source, Bitmap mask, Bitmap overlay)
    {
        var rectangle = new Rectangle(Point.Empty, source.Size);
        var sourceData = source.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var maskData = mask.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        var overlayData = overlay.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            var sourceBytes = CopyBitmapBytes(sourceData);
            var maskBytes = new byte[Math.Abs(maskData.Stride) * maskData.Height];
            var overlayBytes = new byte[Math.Abs(overlayData.Stride) * overlayData.Height];

            for (var y = 0; y < source.Height; y++)
            {
                var sourceRow = y * Math.Abs(sourceData.Stride);
                var maskRow = y * Math.Abs(maskData.Stride);
                var overlayRow = y * Math.Abs(overlayData.Stride);

                for (var x = 0; x < source.Width; x++)
                {
                    var sourceIndex = sourceRow + x * 4;
                    var maskIndex = maskRow + x * 4;
                    var overlayIndex = overlayRow + x * 4;
                    var hasForeground =
                        sourceBytes[sourceIndex] != 0 ||
                        sourceBytes[sourceIndex + 1] != 0 ||
                        sourceBytes[sourceIndex + 2] != 0;

                    maskBytes[maskIndex + 3] = 255;
                    if (!hasForeground)
                    {
                        continue;
                    }

                    maskBytes[maskIndex] = 255;
                    maskBytes[maskIndex + 1] = 255;
                    maskBytes[maskIndex + 2] = 255;

                    overlayBytes[overlayIndex + 2] = 255;
                    overlayBytes[overlayIndex + 3] = 100;
                }
            }

            Marshal.Copy(maskBytes, 0, maskData.Scan0, maskBytes.Length);
            Marshal.Copy(overlayBytes, 0, overlayData.Scan0, overlayBytes.Length);
        }
        finally
        {
            source.UnlockBits(sourceData);
            mask.UnlockBits(maskData);
            overlay.UnlockBits(overlayData);
        }
    }

    private static byte[] CopyBitmapBytes(BitmapData data)
    {
        var bytes = new byte[Math.Abs(data.Stride) * data.Height];
        Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
        return bytes;
    }
}
