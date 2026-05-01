using System.Drawing;
using FeatureVisionDetector.WinForms.Models;

namespace FeatureVisionDetector.WinForms.Services;

public sealed class OverlayRenderer
{
    public Bitmap Render(Image source, IReadOnlyCollection<FeatureResult> results, bool detectionEnabled)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(results);

        var canvas = new Bitmap(source.Width, source.Height);

        using var graphics = Graphics.FromImage(canvas);
        using var boxPen = new Pen(Color.LimeGreen, 2F);
        using var bannerBrush = new SolidBrush(Color.FromArgb(210, 18, 18, 18));
        using var textBrush = new SolidBrush(Color.WhiteSmoke);
        using var indexBrush = new SolidBrush(Color.Yellow);
        using var font = new Font(FontFamily.GenericSansSerif, 10F, FontStyle.Bold);
        using var indexFont = new Font(FontFamily.GenericSansSerif, 8F, FontStyle.Bold);

        graphics.DrawImage(source, Point.Empty);

        if (!detectionEnabled)
        {
            return canvas;
        }

        var index = 1;
        foreach (var result in results.OrderBy(result => result.BoundingBox.X))
        {
            graphics.DrawRectangle(boxPen, result.BoundingBox);
            graphics.DrawString(index.ToString(), indexFont, indexBrush, result.BoundingBox.X, Math.Max(0, result.BoundingBox.Y - 16));
            index++;
        }

        graphics.FillRectangle(bannerBrush, 12, 12, 118, 28);
        graphics.DrawString($"Count: {results.Count}", font, textBrush, 18, 18);

        return canvas;
    }
}
