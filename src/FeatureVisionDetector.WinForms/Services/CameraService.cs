using System.Drawing;

namespace FeatureVisionDetector.WinForms.Services;

public sealed class CameraService : IDisposable
{
    private bool _disposed;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        ThrowIfDisposed();
        IsRunning = true;
    }

    public void Stop()
    {
        if (_disposed)
        {
            return;
        }

        IsRunning = false;
    }

    public Bitmap CreatePreviewFrame(Size requestedSize)
    {
        ThrowIfDisposed();

        var width = Math.Max(requestedSize.Width, 320);
        var height = Math.Max(requestedSize.Height, 240);
        var bitmap = new Bitmap(width, height);

        using var graphics = Graphics.FromImage(bitmap);
        using var accentPen = new Pen(Color.FromArgb(68, 168, 114), 2F);
        using var titleBrush = new SolidBrush(Color.WhiteSmoke);
        using var detailBrush = new SolidBrush(Color.Silver);
        using var titleFont = new Font(FontFamily.GenericSansSerif, 16F, FontStyle.Bold);
        using var detailFont = new Font(FontFamily.GenericSansSerif, 9F, FontStyle.Regular);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        graphics.Clear(Color.FromArgb(28, 30, 36));
        graphics.DrawRectangle(accentPen, 10, 10, width - 20, height - 20);

        var title = IsRunning ? "Camera preview placeholder" : "Camera stopped";
        var detail = IsRunning
            ? "DirectShow capture will be connected in a later implementation phase."
            : "Press Start Camera to initialize the preview surface.";

        graphics.DrawString(title, titleFont, titleBrush, new RectangleF(0, height / 2F - 28F, width, 30F), format);
        graphics.DrawString(detail, detailFont, detailBrush, new RectangleF(0, height / 2F + 12F, width, 24F), format);

        return bitmap;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        IsRunning = false;
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
