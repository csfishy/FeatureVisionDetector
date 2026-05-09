using FeatureVision.AnnotationTool.Forms;
using FeatureVision.AnnotationTool.Models;
using FeatureVision.Core.Models;
using System.Drawing.Drawing2D;

namespace FeatureVision.AnnotationTool.Controls;

internal sealed class ImageAnnotationCanvas : Control
{
    private const float MinimumZoom = 0.05f;
    private const float MaximumZoom = 20.0f;
    private const float MeasurementBoxMinimumSize = 8.0f;
    private const float MeasurementBoxHitToleranceScreen = 8.0f;
    private const float MeasurementScanLineSpacing = 3.0f;
    private const float MeasurementScanStep = 1.0f;
    private const float MeasurementClickMoveToleranceScreen = 3.0f;

    private AnnotatedImage? activeImage;
    private AnnotationToolMode toolMode = AnnotationToolMode.RectangleRoi;
    private Point lastMouseLocation;
    private Point rectangleStart;
    private bool isDrawingRectangle;
    private bool isPainting;
    private bool isPanning;
    private Rectangle currentRectangle;
    private ConnectedComponentResult? componentHighlight;
    private MeasurementBox? selectedMeasurementBox;
    private MeasurementBox? drawingMeasurementBox;
    private MeasurementBoxInteraction measurementBoxInteraction = MeasurementBoxInteraction.None;
    private MeasurementBoxEdge resizingMeasurementBoxEdge = MeasurementBoxEdge.None;
    private PointF measurementBoxStartPoint;
    private PointF measurementBoxStartCenter;
    private float measurementBoxStartWidth;
    private float measurementBoxStartHeight;
    private float measurementBoxStartRotation;
    private bool measurementBoxStartCenterLineFollowsLongSide;
    private float measurementBoxStartCenterLineDirection;
    private float measurementBoxStartSearchDirection;
    private float measurementBoxStartAngle;
    private PointF? pendingMeasurementBoxAnchor;
    private PointF pendingMeasurementBoxPreviewPoint;
    private PointF measurementBoxAxisStart;
    private PointF measurementBoxAxisEnd;
    private bool measurementBoxInteractionMoved;
    private int nextMeasurementBoxId = 1;

    public ImageAnnotationCanvas()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(32, 32, 32);
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    public AnnotationToolMode ToolMode
    {
        get => toolMode;
        set
        {
            toolMode = value;
            if (toolMode != AnnotationToolMode.MeasurementBox)
            {
                ClearPendingMeasurementBoxCreation();
            }

            Cursor = Cursors.Default;
            Invalidate();
        }
    }

    public event EventHandler? MaskChanged;

    public int BrushSize { get; set; } = 24;

    public float Zoom { get; private set; } = 1.0f;

    public PointF Pan { get; private set; }

    public void SetImage(AnnotatedImage? image)
    {
        activeImage = image;
        componentHighlight = null;
        selectedMeasurementBox = null;
        drawingMeasurementBox = null;
        measurementBoxInteraction = MeasurementBoxInteraction.None;
        ClearPendingMeasurementBoxCreation();
        nextMeasurementBoxId = activeImage?.MeasurementBoxes.Count > 0
            ? activeImage.MeasurementBoxes.Max(measurementBox => measurementBox.Id) + 1
            : 1;
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
        DrawMeasurementBoxes(e.Graphics);

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

        if (ToolMode == AnnotationToolMode.MeasurementBox)
        {
            BeginMeasurementBoxInteraction(e.Location);
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

        if (measurementBoxInteraction != MeasurementBoxInteraction.None)
        {
            UpdateMeasurementBoxInteraction(e.Location);
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
            return;
        }

        if (ToolMode == AnnotationToolMode.MeasurementBox &&
            pendingMeasurementBoxAnchor is not null)
        {
            pendingMeasurementBoxPreviewPoint = ClampImagePoint(ClientToImage(e.Location));
            Cursor = Cursors.Cross;
            Invalidate();
            return;
        }

        UpdateCursor(e.Location);
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

        if (measurementBoxInteraction != MeasurementBoxInteraction.None)
        {
            EndMeasurementBoxInteraction();
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

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);

        if (measurementBoxInteraction == MeasurementBoxInteraction.None && !isPanning)
        {
            Cursor = Cursors.Default;
        }
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

    private void BeginMeasurementBoxInteraction(Point clientPoint)
    {
        if (activeImage is null)
        {
            return;
        }

        var imagePoint = ClientToImage(clientPoint);
        if (pendingMeasurementBoxAnchor is PointF pendingAnchor)
        {
            BeginParallelMeasurementBoxCreation(pendingAnchor, imagePoint);
            return;
        }

        var hit = HitTestMeasurementBoxes(imagePoint);
        if (hit.Box is not null)
        {
            selectedMeasurementBox = hit.Box;
            measurementBoxStartPoint = imagePoint;
            measurementBoxStartCenter = hit.Box.Center;
            measurementBoxStartWidth = hit.Box.Width;
            measurementBoxStartHeight = hit.Box.Height;
            measurementBoxStartRotation = hit.Box.RotationDegrees;
            measurementBoxStartCenterLineFollowsLongSide = hit.Box.CenterLineFollowsLongSide;
            measurementBoxStartCenterLineDirection = hit.Box.CenterLineDirectionDegrees;
            measurementBoxStartSearchDirection = hit.Box.SearchDirectionDegrees;

            measurementBoxInteraction = hit.Kind switch
            {
                MeasurementBoxHitKind.Rotate => MeasurementBoxInteraction.Rotating,
                MeasurementBoxHitKind.Resize => MeasurementBoxInteraction.Resizing,
                MeasurementBoxHitKind.Move => MeasurementBoxInteraction.Moving,
                _ => MeasurementBoxInteraction.None
            };
            resizingMeasurementBoxEdge = hit.Edge;
            measurementBoxStartAngle = CalculateAngleDegrees(measurementBoxStartCenter, imagePoint);
            measurementBoxInteractionMoved = false;
            UpdateCursor(clientPoint);
            Invalidate();
            return;
        }

        if (!IsPointInsideActiveImage(imagePoint))
        {
            return;
        }

        selectedMeasurementBox = null;
        measurementBoxStartPoint = ClampImagePoint(imagePoint);
        drawingMeasurementBox = new MeasurementBox
        {
            Id = nextMeasurementBoxId,
            Center = measurementBoxStartPoint,
            Width = 0.0f,
            Height = 0.0f,
            CenterLineFollowsLongSide = true,
            SearchDirectionDegrees = 0.0f
        };
        measurementBoxInteraction = MeasurementBoxInteraction.Creating;
        measurementBoxInteractionMoved = false;
        Cursor = Cursors.Cross;
        Invalidate();
    }

    private void BeginParallelMeasurementBoxCreation(PointF anchorPoint, PointF imagePoint)
    {
        if (activeImage is null)
        {
            return;
        }

        var axisEnd = ClampImagePoint(imagePoint);
        var axisLength = Distance(anchorPoint, axisEnd);
        if (axisLength < MeasurementBoxMinimumSize)
        {
            pendingMeasurementBoxAnchor = axisEnd;
            pendingMeasurementBoxPreviewPoint = axisEnd;
            Invalidate();
            return;
        }

        ClearPendingMeasurementBoxCreation();
        selectedMeasurementBox = null;
        measurementBoxAxisStart = anchorPoint;
        measurementBoxAxisEnd = axisEnd;
        var axisAngle = CalculateAngleDegrees(anchorPoint, axisEnd);
        drawingMeasurementBox = new MeasurementBox
        {
            Id = nextMeasurementBoxId,
            Center = new PointF(
                (anchorPoint.X + axisEnd.X) * 0.5f,
                (anchorPoint.Y + axisEnd.Y) * 0.5f),
            Width = axisLength,
            Height = 0.0f,
            RotationDegrees = axisAngle,
            CenterLineFollowsLongSide = false,
            CenterLineDirectionDegrees = axisAngle,
            SearchDirectionDegrees = axisAngle
        };
        measurementBoxInteraction = MeasurementBoxInteraction.CreatingParallel;
        measurementBoxInteractionMoved = false;
        Cursor = Cursors.Cross;
        Invalidate();
    }

    private void UpdateMeasurementBoxInteraction(Point clientPoint)
    {
        if (activeImage is null)
        {
            return;
        }

        var imagePoint = ClientToImage(clientPoint);
        if (Distance(measurementBoxStartPoint, imagePoint) * Zoom >= MeasurementClickMoveToleranceScreen)
        {
            measurementBoxInteractionMoved = true;
        }

        if (!measurementBoxInteractionMoved &&
            measurementBoxInteraction is MeasurementBoxInteraction.Resizing or
                MeasurementBoxInteraction.Moving or
                MeasurementBoxInteraction.Rotating)
        {
            return;
        }

        switch (measurementBoxInteraction)
        {
            case MeasurementBoxInteraction.Creating:
                UpdateMeasurementBoxCreation(ClampImagePoint(imagePoint));
                break;
            case MeasurementBoxInteraction.CreatingParallel:
                UpdateParallelMeasurementBoxCreation(ClampImagePoint(imagePoint));
                break;
            case MeasurementBoxInteraction.Moving:
                UpdateMeasurementBoxMove(imagePoint);
                break;
            case MeasurementBoxInteraction.Resizing:
                UpdateMeasurementBoxResize(imagePoint);
                break;
            case MeasurementBoxInteraction.Rotating:
                UpdateMeasurementBoxRotation(imagePoint);
                break;
        }

        Invalidate();
    }

    private void EndMeasurementBoxInteraction()
    {
        var interaction = measurementBoxInteraction;

        if (interaction == MeasurementBoxInteraction.Resizing &&
            !measurementBoxInteractionMoved &&
            selectedMeasurementBox is not null &&
            resizingMeasurementBoxEdge != MeasurementBoxEdge.None)
        {
            RestoreSelectedMeasurementBoxStartState();
            ApplyMeasurementBoxEdgeClick(selectedMeasurementBox, resizingMeasurementBoxEdge);
        }
        else if (activeImage is not null &&
            drawingMeasurementBox is { } completedBox &&
            completedBox.Width >= MeasurementBoxMinimumSize &&
            completedBox.Height >= MeasurementBoxMinimumSize)
        {
            selectedMeasurementBox = completedBox;
            activeImage.MeasurementBoxes.Add(completedBox);
            nextMeasurementBoxId++;
        }
        else if (interaction == MeasurementBoxInteraction.Creating &&
            drawingMeasurementBox is not null)
        {
            pendingMeasurementBoxAnchor = measurementBoxStartPoint;
            pendingMeasurementBoxPreviewPoint = measurementBoxStartPoint;
        }
        else if (interaction == MeasurementBoxInteraction.CreatingParallel)
        {
            pendingMeasurementBoxAnchor = measurementBoxAxisStart;
            pendingMeasurementBoxPreviewPoint = measurementBoxAxisEnd;
        }

        drawingMeasurementBox = null;
        measurementBoxInteraction = MeasurementBoxInteraction.None;
        resizingMeasurementBoxEdge = MeasurementBoxEdge.None;
        Cursor = ToolMode == AnnotationToolMode.MeasurementBox ? Cursors.Cross : Cursors.Default;
        Invalidate();
    }

    private void UpdateMeasurementBoxCreation(PointF imagePoint)
    {
        if (drawingMeasurementBox is null)
        {
            return;
        }

        var left = Math.Min(measurementBoxStartPoint.X, imagePoint.X);
        var right = Math.Max(measurementBoxStartPoint.X, imagePoint.X);
        var top = Math.Min(measurementBoxStartPoint.Y, imagePoint.Y);
        var bottom = Math.Max(measurementBoxStartPoint.Y, imagePoint.Y);

        drawingMeasurementBox.Center = new PointF(
            (left + right) * 0.5f,
            (top + bottom) * 0.5f);
        drawingMeasurementBox.Width = right - left;
        drawingMeasurementBox.Height = bottom - top;
        drawingMeasurementBox.RotationDegrees = 0.0f;
        drawingMeasurementBox.CenterLineFollowsLongSide = true;
        if (Distance(measurementBoxStartPoint, imagePoint) >= 1.0f)
        {
            var centerLineDirection = GetCenterLineDirectionDegrees(drawingMeasurementBox);
            drawingMeasurementBox.SearchDirectionDegrees = ChoosePerpendicularDirection(
                centerLineDirection,
                new PointF(
                    imagePoint.X - measurementBoxStartPoint.X,
                    imagePoint.Y - measurementBoxStartPoint.Y));
        }
    }

    private void UpdateParallelMeasurementBoxCreation(PointF imagePoint)
    {
        if (drawingMeasurementBox is null)
        {
            return;
        }

        var axisVector = new PointF(
            measurementBoxAxisEnd.X - measurementBoxAxisStart.X,
            measurementBoxAxisEnd.Y - measurementBoxAxisStart.Y);
        var axisLength = Math.Max(MeasurementBoxMinimumSize, Length(axisVector));
        var unit = new PointF(axisVector.X / axisLength, axisVector.Y / axisLength);
        var normal = new PointF(-unit.Y, unit.X);
        var dragVector = new PointF(
            imagePoint.X - measurementBoxAxisEnd.X,
            imagePoint.Y - measurementBoxAxisEnd.Y);
        var signedHeight = dragVector.X * normal.X + dragVector.Y * normal.Y;
        var offset = new PointF(normal.X * signedHeight, normal.Y * signedHeight);

        drawingMeasurementBox.Center = new PointF(
            (measurementBoxAxisStart.X + measurementBoxAxisEnd.X + offset.X) * 0.5f,
            (measurementBoxAxisStart.Y + measurementBoxAxisEnd.Y + offset.Y) * 0.5f);
        drawingMeasurementBox.Width = axisLength;
        drawingMeasurementBox.Height = Math.Abs(signedHeight);
        drawingMeasurementBox.RotationDegrees = CalculateAngleDegrees(measurementBoxAxisStart, measurementBoxAxisEnd);
        drawingMeasurementBox.CenterLineFollowsLongSide = false;
        drawingMeasurementBox.CenterLineDirectionDegrees = drawingMeasurementBox.RotationDegrees;
        drawingMeasurementBox.SearchDirectionDegrees = ChoosePerpendicularDirection(
            drawingMeasurementBox.CenterLineDirectionDegrees,
            dragVector);
    }

    private void UpdateMeasurementBoxMove(PointF imagePoint)
    {
        if (selectedMeasurementBox is null)
        {
            return;
        }

        selectedMeasurementBox.Center = new PointF(
            measurementBoxStartCenter.X + imagePoint.X - measurementBoxStartPoint.X,
            measurementBoxStartCenter.Y + imagePoint.Y - measurementBoxStartPoint.Y);
    }

    private void UpdateMeasurementBoxResize(PointF imagePoint)
    {
        if (selectedMeasurementBox is null)
        {
            return;
        }

        var startBox = CreateMeasurementBoxSnapshot();
        var localPoint = ImageToMeasurementBoxLocal(startBox, imagePoint);
        var left = -measurementBoxStartWidth * 0.5f;
        var right = measurementBoxStartWidth * 0.5f;
        var top = -measurementBoxStartHeight * 0.5f;
        var bottom = measurementBoxStartHeight * 0.5f;

        switch (resizingMeasurementBoxEdge)
        {
            case MeasurementBoxEdge.Left:
                left = Math.Min(localPoint.X, right - MeasurementBoxMinimumSize);
                break;
            case MeasurementBoxEdge.Right:
                right = Math.Max(localPoint.X, left + MeasurementBoxMinimumSize);
                break;
            case MeasurementBoxEdge.Top:
                top = Math.Min(localPoint.Y, bottom - MeasurementBoxMinimumSize);
                break;
            case MeasurementBoxEdge.Bottom:
                bottom = Math.Max(localPoint.Y, top + MeasurementBoxMinimumSize);
                break;
        }

        var localCenter = new PointF((left + right) * 0.5f, (top + bottom) * 0.5f);
        selectedMeasurementBox.Center = MeasurementBoxLocalToImage(startBox, localCenter);
        selectedMeasurementBox.Width = right - left;
        selectedMeasurementBox.Height = bottom - top;
        selectedMeasurementBox.RotationDegrees = measurementBoxStartRotation;
        selectedMeasurementBox.CenterLineFollowsLongSide = measurementBoxStartCenterLineFollowsLongSide;
        selectedMeasurementBox.CenterLineDirectionDegrees = measurementBoxStartCenterLineDirection;
        selectedMeasurementBox.SearchDirectionDegrees = measurementBoxStartSearchDirection;
    }

    private void RestoreSelectedMeasurementBoxStartState()
    {
        if (selectedMeasurementBox is null)
        {
            return;
        }

        selectedMeasurementBox.Center = measurementBoxStartCenter;
        selectedMeasurementBox.Width = measurementBoxStartWidth;
        selectedMeasurementBox.Height = measurementBoxStartHeight;
        selectedMeasurementBox.RotationDegrees = measurementBoxStartRotation;
        selectedMeasurementBox.CenterLineFollowsLongSide = measurementBoxStartCenterLineFollowsLongSide;
        selectedMeasurementBox.CenterLineDirectionDegrees = measurementBoxStartCenterLineDirection;
        selectedMeasurementBox.SearchDirectionDegrees = measurementBoxStartSearchDirection;
    }

    private static void ApplyMeasurementBoxEdgeClick(
        MeasurementBox measurementBox,
        MeasurementBoxEdge edge)
    {
        var edgeDirection = GetMeasurementBoxEdgeDirectionDegrees(measurementBox, edge);
        var currentCenterLineDirection = GetCenterLineDirectionDegrees(measurementBox);
        var isAlreadyParallel = AreDirectionsParallel(currentCenterLineDirection, edgeDirection);

        measurementBox.CenterLineFollowsLongSide = false;
        measurementBox.CenterLineDirectionDegrees = edgeDirection;

        if (isAlreadyParallel)
        {
            measurementBox.SearchDirectionDegrees = NormalizeDegrees(
                GetSearchDirectionDegrees(measurementBox) + 180.0f);
        }
    }

    private void UpdateMeasurementBoxRotation(PointF imagePoint)
    {
        if (selectedMeasurementBox is null)
        {
            return;
        }

        var currentAngle = CalculateAngleDegrees(measurementBoxStartCenter, imagePoint);
        selectedMeasurementBox.Center = measurementBoxStartCenter;
        selectedMeasurementBox.Width = measurementBoxStartWidth;
        selectedMeasurementBox.Height = measurementBoxStartHeight;
        selectedMeasurementBox.RotationDegrees = NormalizeDegrees(
            measurementBoxStartRotation + currentAngle - measurementBoxStartAngle);
        selectedMeasurementBox.CenterLineDirectionDegrees = NormalizeDegrees(
            measurementBoxStartCenterLineDirection + currentAngle - measurementBoxStartAngle);
        selectedMeasurementBox.SearchDirectionDegrees = NormalizeDegrees(
            measurementBoxStartSearchDirection + currentAngle - measurementBoxStartAngle);
    }

    private void DrawMeasurementBoxes(Graphics graphics)
    {
        if (activeImage is null)
        {
            return;
        }

        var previousSmoothingMode = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        foreach (var measurementBox in activeImage.MeasurementBoxes)
        {
            DrawMeasurementBox(
                graphics,
                measurementBox,
                ToolMode == AnnotationToolMode.MeasurementBox &&
                    ReferenceEquals(measurementBox, selectedMeasurementBox),
                isPreview: false);
        }

        DrawPendingMeasurementBoxLine(graphics);

        if (drawingMeasurementBox is not null)
        {
            DrawMeasurementBox(graphics, drawingMeasurementBox, isSelected: true, isPreview: true);
        }

        graphics.SmoothingMode = previousSmoothingMode;
    }

    private void DrawPendingMeasurementBoxLine(Graphics graphics)
    {
        if (pendingMeasurementBoxAnchor is not PointF anchor)
        {
            return;
        }

        var start = ImageToClient(anchor);
        var end = ImageToClient(pendingMeasurementBoxPreviewPoint);
        using var pen = new Pen(Color.FromArgb(230, Color.LightSkyBlue), 1.6f)
        {
            DashStyle = DashStyle.Dash
        };
        using var brush = new SolidBrush(Color.FromArgb(230, Color.LightSkyBlue));
        if (Distance(anchor, pendingMeasurementBoxPreviewPoint) >= 1.0f)
        {
            graphics.DrawLine(pen, start, end);
        }

        graphics.FillEllipse(brush, start.X - 3.0f, start.Y - 3.0f, 6.0f, 6.0f);
    }

    private void DrawMeasurementBox(
        Graphics graphics,
        MeasurementBox measurementBox,
        bool isSelected,
        bool isPreview)
    {
        if (measurementBox.Width <= 0.0f || measurementBox.Height <= 0.0f)
        {
            return;
        }

        var corners = GetMeasurementBoxCorners(measurementBox)
            .Select(ImageToClient)
            .ToArray();
        using var outlinePen = new Pen(isSelected ? Color.OrangeRed : Color.Red, isSelected ? 2.2f : 1.6f);
        if (isPreview)
        {
            outlinePen.DashStyle = DashStyle.Dash;
        }

        graphics.DrawPolygon(outlinePen, corners);

        var centerLineDirection = DegreesToUnitVector(GetCenterLineDirectionDegrees(measurementBox));
        var centerLineHalfLength = GetDistanceToMeasurementBoxBoundary(measurementBox, centerLineDirection);
        var centerLineStart = ImageToClient(new PointF(
            measurementBox.Center.X - centerLineDirection.X * centerLineHalfLength,
            measurementBox.Center.Y - centerLineDirection.Y * centerLineHalfLength));
        var centerLineEnd = ImageToClient(new PointF(
            measurementBox.Center.X + centerLineDirection.X * centerLineHalfLength,
            measurementBox.Center.Y + centerLineDirection.Y * centerLineHalfLength));
        using var centerPen = new Pen(Color.FromArgb(220, Color.LightCyan), 1.5f);
        graphics.DrawLine(centerPen, centerLineStart, centerLineEnd);

        var arrowDirection = DegreesToUnitVector(GetSearchDirectionDegrees(measurementBox));
        var arrowBoundaryDistance = GetDistanceToMeasurementBoxBoundary(measurementBox, arrowDirection);
        var arrowLength = Math.Max(0.0f, arrowBoundaryDistance - 4.0f / Zoom);
        if (arrowLength > 2.0f)
        {
            var arrowStart = ImageToClient(measurementBox.Center);
            var arrowEnd = ImageToClient(new PointF(
                measurementBox.Center.X + arrowDirection.X * arrowLength,
                measurementBox.Center.Y + arrowDirection.Y * arrowLength));
            using var arrowPen = new Pen(Color.FromArgb(230, Color.LightSkyBlue), 2.4f);
            using var arrowCap = new AdjustableArrowCap(5.0f, 5.0f, true);
            arrowPen.CustomEndCap = arrowCap;
            graphics.DrawLine(arrowPen, arrowStart, arrowEnd);
        }

        DrawMeasurementBoxEdgePoints(graphics, measurementBox);

        if (isSelected)
        {
            DrawMeasurementBoxHandles(graphics, corners);
        }
    }

    private void DrawMeasurementBoxEdgePoints(
        Graphics graphics,
        MeasurementBox measurementBox)
    {
        var edgePoints = CollectMeasurementBoxEdgePoints(measurementBox);
        if (edgePoints.Count == 0)
        {
            return;
        }

        var pointSize = Math.Clamp(3.0f * Zoom, 2.5f, 5.0f);
        using var fillBrush = new SolidBrush(Color.Gold);
        using var outlinePen = new Pen(Color.FromArgb(210, Color.Black), 1.0f);

        foreach (var edgePoint in edgePoints)
        {
            var clientPoint = ImageToClient(edgePoint);
            var bounds = new RectangleF(
                clientPoint.X - pointSize * 0.5f,
                clientPoint.Y - pointSize * 0.5f,
                pointSize,
                pointSize);
            graphics.FillEllipse(fillBrush, bounds);
            graphics.DrawEllipse(outlinePen, bounds);
        }
    }

    private IReadOnlyList<PointF> CollectMeasurementBoxEdgePoints(
        MeasurementBox measurementBox)
    {
        if (activeImage is null ||
            measurementBox.Width < MeasurementBoxMinimumSize ||
            measurementBox.Height < MeasurementBoxMinimumSize)
        {
            return Array.Empty<PointF>();
        }

        var centerLineDirection = DegreesToUnitVector(GetCenterLineDirectionDegrees(measurementBox));
        var searchDirection = DegreesToUnitVector(GetSearchDirectionDegrees(measurementBox));
        var halfCenterLineLength = GetDistanceToMeasurementBoxBoundary(measurementBox, centerLineDirection);
        if (!float.IsFinite(halfCenterLineLength) || halfCenterLineLength <= 0.0f)
        {
            return Array.Empty<PointF>();
        }

        var maxScanDistance = MathF.Sqrt(
            measurementBox.Width * measurementBox.Width +
            measurementBox.Height * measurementBox.Height) * 0.5f + 2.0f;
        var edgePoints = new List<PointF>();

        for (var offset = -halfCenterLineLength;
            offset <= halfCenterLineLength;
            offset += MeasurementScanLineSpacing)
        {
            var scanBase = new PointF(
                measurementBox.Center.X + centerLineDirection.X * offset,
                measurementBox.Center.Y + centerLineDirection.Y * offset);
            var edgePoint = FindStrongestGradientPointOnScanLine(
                measurementBox,
                scanBase,
                searchDirection,
                maxScanDistance);

            if (edgePoint is not null)
            {
                edgePoints.Add(edgePoint.Value);
            }
        }

        return edgePoints;
    }

    private PointF? FindStrongestGradientPointOnScanLine(
        MeasurementBox measurementBox,
        PointF scanBase,
        PointF searchDirection,
        float maxScanDistance)
    {
        var hasEnteredBox = false;
        PointF? strongestPoint = null;
        var strongestGradient = 0.0;

        for (var scanDistance = -maxScanDistance;
            scanDistance <= maxScanDistance;
            scanDistance += MeasurementScanStep)
        {
            var point = new PointF(
                scanBase.X + searchDirection.X * scanDistance,
                scanBase.Y + searchDirection.Y * scanDistance);

            if (!IsPointInsideMeasurementBox(measurementBox, point))
            {
                if (hasEnteredBox)
                {
                    break;
                }

                continue;
            }

            hasEnteredBox = true;
            if (!IsMaskForeground(point))
            {
                continue;
            }

            var gradient = CalculateDirectionalGrayGradient(point, searchDirection);
            if (gradient > strongestGradient)
            {
                strongestGradient = gradient;
                strongestPoint = point;
            }
        }

        return strongestGradient > 0.0 ? strongestPoint : null;
    }

    private static void DrawMeasurementBoxHandles(Graphics graphics, IReadOnlyList<PointF> corners)
    {
        const float handleSize = 6.0f;
        using var fillBrush = new SolidBrush(Color.White);
        using var outlinePen = new Pen(Color.Red, 1.0f);

        foreach (var corner in corners)
        {
            var handleRectangle = new RectangleF(
                corner.X - handleSize * 0.5f,
                corner.Y - handleSize * 0.5f,
                handleSize,
                handleSize);
            graphics.FillRectangle(fillBrush, handleRectangle);
            graphics.DrawRectangle(
                outlinePen,
                handleRectangle.X,
                handleRectangle.Y,
                handleRectangle.Width,
                handleRectangle.Height);
        }
    }

    private void UpdateCursor(Point clientPoint)
    {
        if (ToolMode != AnnotationToolMode.MeasurementBox || activeImage is null)
        {
            Cursor = Cursors.Default;
            return;
        }

        if (measurementBoxInteraction == MeasurementBoxInteraction.Moving)
        {
            Cursor = Cursors.SizeAll;
            return;
        }

        if (measurementBoxInteraction == MeasurementBoxInteraction.Rotating)
        {
            Cursor = Cursors.Hand;
            return;
        }

        if (measurementBoxInteraction == MeasurementBoxInteraction.Resizing)
        {
            Cursor = GetResizeCursor(resizingMeasurementBoxEdge, measurementBoxStartRotation);
            return;
        }

        var hit = HitTestMeasurementBoxes(ClientToImage(clientPoint));
        Cursor = hit.Kind switch
        {
            MeasurementBoxHitKind.Rotate => Cursors.Hand,
            MeasurementBoxHitKind.Resize when hit.Box is not null => GetResizeCursor(hit.Edge, hit.Box.RotationDegrees),
            MeasurementBoxHitKind.Move => Cursors.SizeAll,
            _ => Cursors.Cross
        };
    }

    private MeasurementBoxHit HitTestMeasurementBoxes(PointF imagePoint)
    {
        if (activeImage is null)
        {
            return MeasurementBoxHit.None;
        }

        for (var index = activeImage.MeasurementBoxes.Count - 1; index >= 0; index--)
        {
            var measurementBox = activeImage.MeasurementBoxes[index];
            var hit = HitTestMeasurementBox(measurementBox, imagePoint);
            if (hit.Kind != MeasurementBoxHitKind.None)
            {
                return hit;
            }
        }

        return MeasurementBoxHit.None;
    }

    private MeasurementBoxHit HitTestMeasurementBox(MeasurementBox measurementBox, PointF imagePoint)
    {
        var tolerance = Math.Max(2.0f, MeasurementBoxHitToleranceScreen / Zoom);
        var localPoint = ImageToMeasurementBoxLocal(measurementBox, imagePoint);
        var halfWidth = measurementBox.Width * 0.5f;
        var halfHeight = measurementBox.Height * 0.5f;

        var cornerHit = GetMeasurementBoxCornerHit(localPoint, halfWidth, halfHeight, tolerance);
        if (cornerHit)
        {
            return new MeasurementBoxHit(measurementBox, MeasurementBoxHitKind.Rotate, MeasurementBoxEdge.None);
        }

        var edge = GetMeasurementBoxEdgeHit(localPoint, halfWidth, halfHeight, tolerance);
        if (edge != MeasurementBoxEdge.None)
        {
            return new MeasurementBoxHit(measurementBox, MeasurementBoxHitKind.Resize, edge);
        }

        if (Math.Abs(localPoint.X) <= halfWidth && Math.Abs(localPoint.Y) <= halfHeight)
        {
            return new MeasurementBoxHit(measurementBox, MeasurementBoxHitKind.Move, MeasurementBoxEdge.None);
        }

        return MeasurementBoxHit.None;
    }

    private static bool GetMeasurementBoxCornerHit(
        PointF localPoint,
        float halfWidth,
        float halfHeight,
        float tolerance)
    {
        var toleranceSquared = tolerance * tolerance * 2.25f;
        var corners = new[]
        {
            new PointF(-halfWidth, -halfHeight),
            new PointF(halfWidth, -halfHeight),
            new PointF(halfWidth, halfHeight),
            new PointF(-halfWidth, halfHeight)
        };

        return corners.Any(corner =>
        {
            var dx = localPoint.X - corner.X;
            var dy = localPoint.Y - corner.Y;
            return dx * dx + dy * dy <= toleranceSquared;
        });
    }

    private static MeasurementBoxEdge GetMeasurementBoxEdgeHit(
        PointF localPoint,
        float halfWidth,
        float halfHeight,
        float tolerance)
    {
        var bestEdge = MeasurementBoxEdge.None;
        var bestDistance = float.MaxValue;

        TryPickEdge(
            MeasurementBoxEdge.Left,
            Math.Abs(localPoint.X + halfWidth),
            Math.Abs(localPoint.Y) <= halfHeight + tolerance);
        TryPickEdge(
            MeasurementBoxEdge.Right,
            Math.Abs(localPoint.X - halfWidth),
            Math.Abs(localPoint.Y) <= halfHeight + tolerance);
        TryPickEdge(
            MeasurementBoxEdge.Top,
            Math.Abs(localPoint.Y + halfHeight),
            Math.Abs(localPoint.X) <= halfWidth + tolerance);
        TryPickEdge(
            MeasurementBoxEdge.Bottom,
            Math.Abs(localPoint.Y - halfHeight),
            Math.Abs(localPoint.X) <= halfWidth + tolerance);

        return bestDistance <= tolerance ? bestEdge : MeasurementBoxEdge.None;

        void TryPickEdge(MeasurementBoxEdge edge, float distance, bool isWithinSpan)
        {
            if (!isWithinSpan || distance >= bestDistance)
            {
                return;
            }

            bestDistance = distance;
            bestEdge = edge;
        }
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

    private PointF ImageToClient(PointF point)
    {
        return new PointF(
            Pan.X + point.X * Zoom,
            Pan.Y + point.Y * Zoom);
    }

    private bool IsPointInsideActiveImage(PointF point)
    {
        return activeImage is not null &&
            point.X >= 0.0f &&
            point.Y >= 0.0f &&
            point.X < activeImage.Image.Width &&
            point.Y < activeImage.Image.Height;
    }

    private static bool IsPointInsideMeasurementBox(
        MeasurementBox measurementBox,
        PointF point)
    {
        var localPoint = ImageToMeasurementBoxLocal(measurementBox, point);
        return Math.Abs(localPoint.X) <= measurementBox.Width * 0.5f &&
            Math.Abs(localPoint.Y) <= measurementBox.Height * 0.5f;
    }

    private bool IsMaskForeground(PointF point)
    {
        if (activeImage is null)
        {
            return false;
        }

        var x = (int)MathF.Round(point.X);
        var y = (int)MathF.Round(point.Y);
        if (x < 0 ||
            y < 0 ||
            x >= activeImage.Mask.Width ||
            y >= activeImage.Mask.Height)
        {
            return false;
        }

        var color = activeImage.Mask.GetPixel(x, y);
        return color.R != 0 || color.G != 0 || color.B != 0;
    }

    private double CalculateDirectionalGrayGradient(
        PointF point,
        PointF searchDirection)
    {
        var backwardPoint = new PointF(
            point.X - searchDirection.X * MeasurementScanStep,
            point.Y - searchDirection.Y * MeasurementScanStep);
        var forwardPoint = new PointF(
            point.X + searchDirection.X * MeasurementScanStep,
            point.Y + searchDirection.Y * MeasurementScanStep);
        var backwardGray = GetImageGrayValue(backwardPoint);
        var forwardGray = GetImageGrayValue(forwardPoint);

        return backwardGray is null || forwardGray is null
            ? 0.0
            : Math.Abs(forwardGray.Value - backwardGray.Value);
    }

    private double? GetImageGrayValue(PointF point)
    {
        if (activeImage is null)
        {
            return null;
        }

        var x = (int)MathF.Round(point.X);
        var y = (int)MathF.Round(point.Y);
        if (x < 0 ||
            y < 0 ||
            x >= activeImage.Image.Width ||
            y >= activeImage.Image.Height)
        {
            return null;
        }

        var color = activeImage.Image.GetPixel(x, y);
        return color.R * 0.299 + color.G * 0.587 + color.B * 0.114;
    }

    private PointF ClampImagePoint(PointF point)
    {
        if (activeImage is null)
        {
            return point;
        }

        return new PointF(
            Math.Clamp(point.X, 0.0f, activeImage.Image.Width),
            Math.Clamp(point.Y, 0.0f, activeImage.Image.Height));
    }

    private MeasurementBox CreateMeasurementBoxSnapshot()
    {
        return new MeasurementBox
        {
            Center = measurementBoxStartCenter,
            Width = measurementBoxStartWidth,
            Height = measurementBoxStartHeight,
            RotationDegrees = measurementBoxStartRotation,
            CenterLineFollowsLongSide = measurementBoxStartCenterLineFollowsLongSide,
            CenterLineDirectionDegrees = measurementBoxStartCenterLineDirection,
            SearchDirectionDegrees = measurementBoxStartSearchDirection
        };
    }

    private void ClearPendingMeasurementBoxCreation()
    {
        pendingMeasurementBoxAnchor = null;
        pendingMeasurementBoxPreviewPoint = PointF.Empty;
    }

    private static PointF[] GetMeasurementBoxCorners(MeasurementBox measurementBox)
    {
        var halfWidth = measurementBox.Width * 0.5f;
        var halfHeight = measurementBox.Height * 0.5f;

        return new[]
        {
            MeasurementBoxLocalToImage(measurementBox, new PointF(-halfWidth, -halfHeight)),
            MeasurementBoxLocalToImage(measurementBox, new PointF(halfWidth, -halfHeight)),
            MeasurementBoxLocalToImage(measurementBox, new PointF(halfWidth, halfHeight)),
            MeasurementBoxLocalToImage(measurementBox, new PointF(-halfWidth, halfHeight))
        };
    }

    private static PointF ImageToMeasurementBoxLocal(MeasurementBox measurementBox, PointF imagePoint)
    {
        var radians = DegreesToRadians(-measurementBox.RotationDegrees);
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        var dx = imagePoint.X - measurementBox.Center.X;
        var dy = imagePoint.Y - measurementBox.Center.Y;

        return new PointF(
            dx * cos - dy * sin,
            dx * sin + dy * cos);
    }

    private static PointF MeasurementBoxLocalToImage(MeasurementBox measurementBox, PointF localPoint)
    {
        var radians = DegreesToRadians(measurementBox.RotationDegrees);
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        return new PointF(
            measurementBox.Center.X + localPoint.X * cos - localPoint.Y * sin,
            measurementBox.Center.Y + localPoint.X * sin + localPoint.Y * cos);
    }

    private static float CalculateAngleDegrees(PointF center, PointF point)
    {
        return MathF.Atan2(point.Y - center.Y, point.X - center.X) * 180.0f / MathF.PI;
    }

    private static PointF DegreesToUnitVector(float angle)
    {
        var radians = DegreesToRadians(angle);
        return new PointF(MathF.Cos(radians), MathF.Sin(radians));
    }

    private static float GetCenterLineDirectionDegrees(MeasurementBox measurementBox)
    {
        if (!measurementBox.CenterLineFollowsLongSide)
        {
            return NormalizeDegrees(measurementBox.CenterLineDirectionDegrees);
        }

        return NormalizeDegrees(measurementBox.RotationDegrees +
            (measurementBox.Width >= measurementBox.Height ? 0.0f : 90.0f));
    }

    private static float GetSearchDirectionDegrees(MeasurementBox measurementBox)
    {
        return ChoosePerpendicularDirection(
            GetCenterLineDirectionDegrees(measurementBox),
            DegreesToUnitVector(measurementBox.SearchDirectionDegrees));
    }

    private static float GetMeasurementBoxEdgeDirectionDegrees(
        MeasurementBox measurementBox,
        MeasurementBoxEdge edge)
    {
        return NormalizeDegrees(measurementBox.RotationDegrees +
            (edge is MeasurementBoxEdge.Left or MeasurementBoxEdge.Right ? 90.0f : 0.0f));
    }

    private static bool AreDirectionsParallel(
        float firstDirectionDegrees,
        float secondDirectionDegrees)
    {
        var delta = Math.Abs(NormalizeDegrees(firstDirectionDegrees - secondDirectionDegrees));
        return delta <= 2.0f || Math.Abs(delta - 180.0f) <= 2.0f;
    }

    private static float ChoosePerpendicularDirection(
        float centerLineDirectionDegrees,
        PointF preferredDirection)
    {
        var normalDirection = DegreesToUnitVector(centerLineDirectionDegrees + 90.0f);
        var dot = normalDirection.X * preferredDirection.X + normalDirection.Y * preferredDirection.Y;

        return NormalizeDegrees(centerLineDirectionDegrees + (dot < 0.0f ? -90.0f : 90.0f));
    }

    private static float GetDistanceToMeasurementBoxBoundary(
        MeasurementBox measurementBox,
        PointF imageDirection)
    {
        var localDirection = ImageVectorToMeasurementBoxLocal(measurementBox, imageDirection);
        var halfWidth = measurementBox.Width * 0.5f;
        var halfHeight = measurementBox.Height * 0.5f;
        var distanceX = Math.Abs(localDirection.X) < 0.0001f
            ? float.PositiveInfinity
            : halfWidth / Math.Abs(localDirection.X);
        var distanceY = Math.Abs(localDirection.Y) < 0.0001f
            ? float.PositiveInfinity
            : halfHeight / Math.Abs(localDirection.Y);

        return Math.Min(distanceX, distanceY);
    }

    private static PointF ImageVectorToMeasurementBoxLocal(
        MeasurementBox measurementBox,
        PointF imageVector)
    {
        var radians = DegreesToRadians(-measurementBox.RotationDegrees);
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        return new PointF(
            imageVector.X * cos - imageVector.Y * sin,
            imageVector.X * sin + imageVector.Y * cos);
    }

    private static float Distance(PointF first, PointF second)
    {
        return Length(new PointF(second.X - first.X, second.Y - first.Y));
    }

    private static float Length(PointF vector)
    {
        return MathF.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
    }

    private static float NormalizeDegrees(float angle)
    {
        while (angle <= -180.0f)
        {
            angle += 360.0f;
        }

        while (angle > 180.0f)
        {
            angle -= 360.0f;
        }

        return angle;
    }

    private static float DegreesToRadians(float angle)
    {
        return angle * MathF.PI / 180.0f;
    }

    private static Cursor GetResizeCursor(MeasurementBoxEdge edge, float rotationDegrees)
    {
        var axisDegrees = edge is MeasurementBoxEdge.Top or MeasurementBoxEdge.Bottom
            ? rotationDegrees + 90.0f
            : rotationDegrees;
        var normalized = axisDegrees % 180.0f;
        if (normalized < 0.0f)
        {
            normalized += 180.0f;
        }

        if (normalized is < 22.5f or >= 157.5f)
        {
            return Cursors.SizeWE;
        }

        if (normalized < 67.5f)
        {
            return Cursors.SizeNWSE;
        }

        if (normalized < 112.5f)
        {
            return Cursors.SizeNS;
        }

        return Cursors.SizeNESW;
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

    private readonly record struct MeasurementBoxHit(
        MeasurementBox? Box,
        MeasurementBoxHitKind Kind,
        MeasurementBoxEdge Edge)
    {
        public static MeasurementBoxHit None { get; } = new(
            null,
            MeasurementBoxHitKind.None,
            MeasurementBoxEdge.None);
    }

    private enum MeasurementBoxInteraction
    {
        None,
        Creating,
        CreatingParallel,
        Moving,
        Resizing,
        Rotating
    }

    private enum MeasurementBoxHitKind
    {
        None,
        Move,
        Resize,
        Rotate
    }

    private enum MeasurementBoxEdge
    {
        None,
        Left,
        Right,
        Top,
        Bottom
    }
}
