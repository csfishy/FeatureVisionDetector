using System.Drawing.Imaging;
using FeatureVision.Core.Models;

namespace FeatureVision.AnnotationTool.Models;

internal sealed class AnnotatedImage : IDisposable
{
    public AnnotatedImage(string imagePath)
    {
        ImagePath = imagePath;
        DisplayName = Path.GetFileName(imagePath);
        Image = new Bitmap(imagePath);
        Mask = CreateMask(Image.Size);
        Overlay = CreateOverlay(Image.Size);
    }

    public string ImagePath { get; }

    public string DisplayName { get; }

    public Bitmap Image { get; }

    public Bitmap Mask { get; }

    public Bitmap Overlay { get; }

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
}
