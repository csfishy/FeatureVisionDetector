using FeatureVision.AnnotationTool.Forms;
using FeatureVision.AnnotationTool.Models;
using FeatureVision.Core.Models;
using System.Drawing.Drawing2D;

namespace FeatureVision.AnnotationTool.Controls;

internal sealed class ImageAnnotationCanvas : Control
{
    private const float MinimumZoom = 0.05f;
    private const float MaximumZoom = 20.0f;

    private AnnotatedImage? activeImage;
    private Point lastMouseLocation;
    private Point rectangleStart;
    private bool isDrawingRectangle;
    private bool isPainting;
    private bool isPanning;
    private Rectangle currentRectangle;
    private ConnectedComponentResult? componentHighlight;

    public ImageAnnotationCanvas()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(32, 32, 32);
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    public AnnotationToolMode ToolMode { get; set; } = AnnotationToolMode.RectangleRoi;

    public event EventHandler? MaskChanged;

    public int BrushSize { get; set; } = 24;

    public float Zoom { get; private set; } = 1.0f;

    public PointF Pan { get; private set; }

    public void SetImage(AnnotatedImage? image)
    {
        activeImage = image;
        componentHighlight = null;
        FitToView();
        Invalidate();
    }

    public void SetComponentHighlight(ConnectedComponentResult? component)
    {
        componentHighlight = component;
        Invalidate();
    }

    public void FitToView()
    {
        if (activeImage is null || ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            Zoom = 1.0f;
            Pan = PointF.Empty;
            return;
        }

        var scaleX = ClientSize.Width / (float)activeImage.Image.Width;
        var scaleY = ClientSize.Height / (float)activeImage.Image.Height;
        Zoom = Math.Clamp(Math.Min(scaleX, scaleY), MinimumZoom, MaximumZoom);

        Pan = new PointF(
            (ClientSize.Width - activeImage.Image.Width * Zoom) / 2.0f,
            (ClientSize.Height - activeImage.Image.Height * Zoom) / 2.0f);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (activeImage is not null && Pan == PointF.Empty)
        {
            FitToView();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.Clear(BackColor);
        if (activeImage is null)
        {
            DrawEmptyState(e.Graphics);
            return;
        }

        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

        var destination = GetImageDestination(activeImage.Image.Size);
        e.Graphics.DrawImage(activeImage.Image, destination);
        e.Graphics.DrawImage(activeImage.Overlay, destination);

        if (isDrawingRectangle && !currentRectangle.IsEmpty)
        {
            using var pen = new Pen(Color.FromArgb(220, Color.Lime), 2.0f);
            e.Graphics.DrawRectangle(pen, ImageRectToClient(currentRectangle));
        }

        DrawComponentHighlight(e.Graphics);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        if (activeImage is null)
        {
            return;
        }

        var imagePoint = ClientToImage(e.Location);
        var zoomFactor = e.Delta > 0 ? 1.15f : 1.0f / 1.15f;
        Zoom = Math.Clamp(Zoom * zoomFactor, MinimumZoom, MaximumZoom);
        Pan = new PointF(
            e.X - imagePoint.X * Zoom,
            e.Y - imagePoint.Y * Zoom);

        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        lastMouseLocation = e.Location;

        if (activeImage is null)
        {
            return;
        }

        if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right)
        {
            isPanning = true;
            Cursor = Cursors.SizeAll;
            return;
        }

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (ToolMode == AnnotationToolMode.RectangleRoi)
        {
            isDrawingRectangle = true;
            rectangleStart = Point.Round(ClientToImage(e.Location));
            currentRectangle = Rectangle.Empty;
            return;
        }

        if (ToolMode is AnnotationToolMode.Brush or AnnotationToolMode.Eraser)
        {
            isPainting = true;
            PaintAt(e.Location);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (activeImage is null)
        {
            return;
        }

        if (isPanning)
        {
            Pan = new PointF(
                Pan.X + e.X - lastMouseLocation.X,
                Pan.Y + e.Y - lastMouseLocation.Y);
            lastMouseLocation = e.Location;
            Invalidate();
            return;
        }

        if (isDrawingRectangle)
        {
            var current = Point.Round(ClientToImage(e.Location));
            currentRectangle = NormalizeRectangle(rectangleStart, current, activeImage.Image.Size);
            Invalidate();
            return;
        }

        if (isPainting)
        {
            PaintAt(e.Location);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (isPanning)
        {
            isPanning = false;
            Cursor = Cursors.Default;
            return;
        }

        if (activeImage is null)
        {
            return;
        }

        if (isDrawingRectangle)
        {
            isDrawingRectangle = false;
            if (!currentRectangle.IsEmpty)
            {
                FillMaskRectangle(currentRectangle);
                OnMaskChanged();
            }

            currentRectangle = Rectangle.Empty;
            Invalidate();
        }

        isPainting = false;
    }

    private void PaintAt(Point clientPoint)
    {
        if (activeImage is null)
        {
            return;
        }

        var imagePoint = ClientToImage(clientPoint);
        var radius = Math.Max(1, BrushSize / 2);
        var rectangle = new Rectangle(
            (int)Math.Round(imagePoint.X) - radius,
            (int)Math.Round(imagePoint.Y) - radius,
            radius * 2,
            radius * 2);

        FillMaskEllipse(rectangle, ToolMode == AnnotationToolMode.Brush);
        OnMaskChanged();
        Invalidate();
    }

    private void FillMaskRectangle(Rectangle rectangle)
    {
        if (activeImage is null)
        {
            return;
        }

        using var maskGraphics = Graphics.FromImage(activeImage.Mask);
        using var overlayGraphics = Graphics.FromImage(activeImage.Overlay);
        maskGraphics.FillRectangle(Brushes.White, rectangle);
        overlayGraphics.CompositingMode = CompositingMode.SourceCopy;
        using var overlayBrush = new SolidBrush(Color.FromArgb(100, Color.Red));
        overlayGraphics.FillRectangle(overlayBrush, rectangle);
    }

    private void FillMaskEllipse(Rectangle rectangle, bool paintForeground)
    {
        if (activeImage is null)
        {
            return;
        }

        using var maskGraphics = Graphics.FromImage(activeImage.Mask);
        using var overlayGraphics = Graphics.FromImage(activeImage.Overlay);

        maskGraphics.SmoothingMode = SmoothingMode.None;
        overlayGraphics.SmoothingMode = SmoothingMode.None;

        if (paintForeground)
        {
            maskGraphics.FillEllipse(Brushes.White, rectangle);
            overlayGraphics.CompositingMode = CompositingMode.SourceCopy;
            using var overlayBrush = new SolidBrush(Color.FromArgb(100, Color.Red));
            overlayGraphics.FillEllipse(overlayBrush, rectangle);
            return;
        }

        overlayGraphics.CompositingMode = CompositingMode.SourceCopy;
        using var transparentBrush = new SolidBrush(Color.Transparent);
        maskGraphics.FillEllipse(Brushes.Black, rectangle);
        overlayGraphics.FillEllipse(transparentBrush, rectangle);
    }

    private RectangleF GetImageDestination(Size imageSize)
    {
        return new RectangleF(
            Pan.X,
            Pan.Y,
            imageSize.Width * Zoom,
            imageSize.Height * Zoom);
    }

    private PointF ClientToImage(Point point)
    {
        return new PointF(
            (point.X - Pan.X) / Zoom,
            (point.Y - Pan.Y) / Zoom);
    }

    private Rectangle ImageRectToClient(Rectangle rectangle)
    {
        return new Rectangle(
            (int)Math.Round(Pan.X + rectangle.X * Zoom),
            (int)Math.Round(Pan.Y + rectangle.Y * Zoom),
            (int)Math.Round(rectangle.Width * Zoom),
            (int)Math.Round(rectangle.Height * Zoom));
    }

    private static Rectangle NormalizeRectangle(Point start, Point end, Size bounds)
    {
        var left = Math.Clamp(Math.Min(start.X, end.X), 0, bounds.Width - 1);
        var top = Math.Clamp(Math.Min(start.Y, end.Y), 0, bounds.Height - 1);
        var right = Math.Clamp(Math.Max(start.X, end.X), 0, bounds.Width);
        var bottom = Math.Clamp(Math.Max(start.Y, end.Y), 0, bounds.Height);

        if (right <= left || bottom <= top)
        {
            return Rectangle.Empty;
        }

        return Rectangle.FromLTRB(left, top, right, bottom);
    }

    private static void DrawEmptyState(Graphics graphics)
    {
        const string message = "Open images to begin annotation.";
        using var brush = new SolidBrush(Color.FromArgb(190, Color.White));
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        graphics.DrawString(message, SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont, brush, graphics.VisibleClipBounds, format);
    }

    private void DrawComponentHighlight(Graphics graphics)
    {
        if (componentHighlight is null)
        {
            return;
        }

        var box = componentHighlight.BoundingBox;
        var rectangle = ImageRectToClient(new Rectangle(box.X, box.Y, box.Width, box.Height));
        using var boxPen = new Pen(Color.Orange, 2.0f);
        using var centerPen = new Pen(Color.Cyan, 2.0f);
        using var labelBrush = new SolidBrush(Color.Orange);
        using var labelBackgroundBrush = new SolidBrush(Color.FromArgb(170, Color.Black));
        var font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;

        graphics.DrawRectangle(boxPen, rectangle);

        var centerX = (float)(Pan.X + componentHighlight.Center.X * Zoom);
        var centerY = (float)(Pan.Y + componentHighlight.Center.Y * Zoom);
        graphics.DrawLine(centerPen, centerX - 6, centerY, centerX + 6, centerY);
        graphics.DrawLine(centerPen, centerX, centerY - 6, centerX, centerY + 6);

        var label = $"#{componentHighlight.Id} {componentHighlight.Score:0.000}";
        var labelSize = graphics.MeasureString(label, font);
        var labelBounds = new RectangleF(
            rectangle.Left,
            Math.Max(0, rectangle.Top - labelSize.Height),
            labelSize.Width,
            labelSize.Height);
        graphics.FillRectangle(labelBackgroundBrush, labelBounds);
        graphics.DrawString(label, font, labelBrush, labelBounds.Location);
    }

    private void OnMaskChanged()
    {
        MaskChanged?.Invoke(this, EventArgs.Empty);
    }
}
