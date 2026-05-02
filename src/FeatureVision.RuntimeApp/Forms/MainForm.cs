using FeatureVision.Camera;
using FeatureVision.Core.Detection;
using FeatureVision.Core.IO;
using FeatureVision.Core.Matching;
using FeatureVision.Core.Models;
using OpenCvSharp;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace FeatureVision.RuntimeApp.Forms;

public partial class MainForm : Form
{
    private readonly CameraService cameraService = new();
    private readonly FeatureFileReader featureFileReader = new();
    private readonly BlackHatComponentDetector blackHatComponentDetector = new();
    private readonly DetectionSettings previewSettings = new();
    private readonly object referenceShapeLock = new();
    private readonly List<Mat> referenceShapeMasks = new();
    private FeatureFileManifest? loadedManifest;
    private string? loadedPackagePath;
    private string? testImagePath;
    private Bitmap? testImageBitmap;
    private string? packageAssetDirectory;
    private CancellationTokenSource? cameraCancellationTokenSource;
    private Task? cameraPreviewTask;
    private DateTime lastCameraFrameDisplayedUtc = DateTime.MinValue;
    private volatile bool isDetectionEnabled;
    private volatile bool isFeatureOverlayEnabled;
    private int pendingPreviewUpdates;
    private IReadOnlyList<DetectionResult> lastStaticResults = Array.Empty<DetectionResult>();
    private IReadOnlyList<ConnectedComponentResult> lastComponents = Array.Empty<ConnectedComponentResult>();
    private bool isUpdatingSettingsUi;

    public MainForm()
    {
        InitializeComponent();
        InitializeResultsGrid();
        InitializeComponentsGrid();
    }

    public async Task LoadFeaturePackageAsync(
        string packagePath,
        CancellationToken cancellationToken = default)
    {
        StopCamera();

        var manifest = await featureFileReader.ReadAsync(packagePath, cancellationToken)
            .ConfigureAwait(true);

        DisposeReferenceShapeMasks();
        DisposePackageAssets();
        packageAssetDirectory = CreatePackageAssetDirectory();
        await ExtractPackageAssetsAsync(packagePath, manifest, packageAssetDirectory, cancellationToken)
            .ConfigureAwait(true);
        LoadReferenceShapeMasks(manifest);

        loadedManifest = manifest;
        loadedPackagePath = packagePath;
        SyncSettingsUiFromSettings(manifest.DetectionSettings);
        packageStatusLabel.Text = $"Package: {Path.GetFileName(packagePath)}";
    }

    public void StartCamera(int cameraIndex = 0)
    {
        if (cameraCancellationTokenSource is not null)
        {
            return;
        }

        var cancellationTokenSource = new CancellationTokenSource();
        cameraCancellationTokenSource = cancellationTokenSource;
        startCameraButton.Enabled = false;
        stopCameraButton.Enabled = true;
        imageStatusLabel.Text = "Camera: starting";

        cameraPreviewTask = Task.Run(
            () => RunCameraPreviewAsync(cameraIndex, cancellationTokenSource),
            cancellationTokenSource.Token);
    }

    public void StopCamera()
    {
        var cancellationTokenSource = cameraCancellationTokenSource;
        if (cancellationTokenSource is null)
        {
            return;
        }

        cameraCancellationTokenSource = null;
        cancellationTokenSource.Cancel();

        startCameraButton.Enabled = true;
        stopCameraButton.Enabled = false;
        imageStatusLabel.Text = "Camera: stopped";
    }

    public void SetDetectionEnabled(bool enabled)
    {
        isDetectionEnabled = enabled;
        enableDetectionCheckBox.Checked = enabled;

        if (!enabled)
        {
            ClearLiveResultStatus(processingText: "Processing: -");
        }
    }

    private void InitializeResultsGrid()
    {
        resultsGridView.Columns.Clear();
        resultsGridView.Columns.Add("CenterX", "CenterX");
        resultsGridView.Columns.Add("CenterY", "CenterY");
        resultsGridView.Columns.Add("Angle", "Angle");
        resultsGridView.Columns.Add("Score", "Score");
        resultsGridView.Columns.Add("Scale", "Scale");
        resultsGridView.Columns.Add("Transform", "Mode");
        resultsGridView.Columns.Add("BoundingBox", "BoundingBox");
        resultsGridView.MultiSelect = false;
        resultsGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    }

    private void InitializeComponentsGrid()
    {
        componentsGridView.Columns.Clear();
        componentsGridView.Columns.Add("Id", "Id");
        componentsGridView.Columns.Add("CenterX", "CenterX");
        componentsGridView.Columns.Add("CenterY", "CenterY");
        componentsGridView.Columns.Add("Angle", "Angle");
        componentsGridView.Columns.Add("Score", "Score");
        componentsGridView.Columns.Add("Shape", "Shape");
        componentsGridView.Columns.Add("Transform", "Mode");
        componentsGridView.Columns.Add("Response", "Resp");
        componentsGridView.Columns.Add("Scale", "Scale");
        componentsGridView.Columns.Add("Area", "Area");
        componentsGridView.Columns.Add("Aspect", "Aspect");
        componentsGridView.Columns.Add("BoundingBox", "BoundingBox");
        componentsGridView.MultiSelect = false;
        componentsGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    }

    private async void LoadPackageButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Load Feature Package",
            Filter = "Feature Package|*.featurepkg|All Files|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            loadPackageButton.Enabled = false;
            await LoadFeaturePackageAsync(dialog.FileName);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Load Package", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadPackageButton.Enabled = true;
        }
    }

    private void LoadTestImageButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Load Test Image",
            Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff|All Files|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            LoadTestImage(dialog.FileName);
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or OutOfMemoryException)
        {
            MessageBox.Show(this, ex.Message, "Load Test Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RunMatchButton_Click(object? sender, EventArgs e)
    {
        try
        {
            RunStaticMatch();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or OpenCVException)
        {
            MessageBox.Show(this, ex.Message, "Run Match", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void FindComponentsButton_Click(object? sender, EventArgs e)
    {
        try
        {
            RunBlackHatComponents();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or OpenCVException)
        {
            MessageBox.Show(this, ex.Message, "Find Components", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void EnableDetectionCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        isDetectionEnabled = enableDetectionCheckBox.Checked;
        if (!isDetectionEnabled)
        {
            ClearLiveResultStatus(processingText: "Processing: -");
        }
    }

    private void PreviewStageComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        RefreshStaticPreview();
    }

    private void ShowFeatureOverlayCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        isFeatureOverlayEnabled = showFeatureOverlayCheckBox.Checked;
        RefreshStaticPreview();
    }

    private void ProcessingSettings_ValueChanged(object? sender, EventArgs e)
    {
        if (isUpdatingSettingsUi)
        {
            return;
        }

        ApplySettingsUiToSettings(CurrentSettings);
        RefreshStaticPreview();
    }

    private void ComponentSettings_ValueChanged(object? sender, EventArgs e)
    {
        if (isUpdatingSettingsUi)
        {
            return;
        }

        ApplySettingsUiToSettings(CurrentSettings);
        if (!string.IsNullOrWhiteSpace(testImagePath) && cameraCancellationTokenSource is null)
        {
            try
            {
                RunBlackHatComponents(selectComponentsTab: false);
                return;
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or OpenCVException)
            {
                resultStatusLabel.Text = $"Components: {ex.Message}";
            }
        }

        RefreshStaticPreview();
    }

    private void ResultsGridView_SelectionChanged(object? sender, EventArgs e)
    {
        RefreshStaticPreview();
    }

    private void ComponentsGridView_SelectionChanged(object? sender, EventArgs e)
    {
        UpdateSelectedComponentStatus();
        RefreshStaticPreview();
    }

    private void ResultsTabControl_SelectedIndexChanged(object? sender, EventArgs e)
    {
        RefreshStaticPreview();
    }

    private void StartCameraButton_Click(object? sender, EventArgs e)
    {
        StartCamera();
    }

    private void StopCameraButton_Click(object? sender, EventArgs e)
    {
        StopCamera();
    }

    private void LoadTestImage(string imagePath)
    {
        StopCamera();

        using var loadedBitmap = new Bitmap(imagePath);

        testImageBitmap?.Dispose();
        testImageBitmap = new Bitmap(loadedBitmap);
        testImagePath = imagePath;
        imageStatusLabel.Text = $"Image: {Path.GetFileName(imagePath)}";
        resultStatusLabel.Text = "Results: 0";
        resultsGridView.Rows.Clear();
        componentsGridView.Rows.Clear();
        lastStaticResults = Array.Empty<DetectionResult>();
        lastComponents = Array.Empty<ConnectedComponentResult>();
        SetPreviewImage(new Bitmap(testImageBitmap));
    }

    private void RunStaticMatch()
    {
        if (string.IsNullOrWhiteSpace(testImagePath))
        {
            throw new InvalidOperationException("Load a test image before running matching.");
        }

        using var frame = Cv2.ImRead(testImagePath, ImreadModes.Color);
        if (frame.Empty())
        {
            throw new IOException("The test image could not be loaded.");
        }

        ApplySettingsUiToSettings(CurrentSettings);
        List<ConnectedComponentResult> components;
        List<ConnectedComponentResult> matchedComponents;
        lock (referenceShapeLock)
        {
            components = blackHatComponentDetector
                .Detect(frame, CurrentSettings, referenceShapeMasks, applyScaleFilter: false)
                .OrderByDescending(component => component.Score)
                .ToList();
            matchedComponents = components
                .Where(component => IsAcceptedMatchCandidate(component, CurrentSettings))
                .Take(CurrentSettings.MaximumDetections)
                .ToList();
        }
        var results = matchedComponents
            .Select(ConvertComponentToDetectionResult)
            .ToList();

        lastComponents = components;
        lastStaticResults = results;
        PopulateComponents(components);
        PopulateResults(results);
        resultsTabControl.SelectedTab = matchesTabPage;
        RefreshStaticPreview();
        resultStatusLabel.Text = $"Results: {results.Count} / Components: {components.Count}";
    }

    private void RunBlackHatComponents(bool selectComponentsTab = true)
    {
        if (string.IsNullOrWhiteSpace(testImagePath))
        {
            throw new InvalidOperationException("Load a test image before finding BlackHat components.");
        }

        ApplySettingsUiToSettings(CurrentSettings);
        using var frame = Cv2.ImRead(testImagePath, ImreadModes.Color);
        if (frame.Empty())
        {
            throw new IOException("The test image could not be loaded.");
        }

        List<ConnectedComponentResult> components;
        lock (referenceShapeLock)
        {
            components = blackHatComponentDetector
                .Detect(frame, CurrentSettings, referenceShapeMasks, applyScaleFilter: false)
                .OrderByDescending(component => component.Score)
                .ToList();
        }

        lastComponents = components;
        PopulateComponents(components);
        if (selectComponentsTab)
        {
            resultsTabControl.SelectedTab = componentsTabPage;
        }

        RefreshStaticPreview();
        resultStatusLabel.Text = $"Components: {components.Count}";
    }

    private void PopulateResults(IReadOnlyList<DetectionResult> results)
    {
        resultsGridView.Rows.Clear();
        foreach (var result in results)
        {
            var rowIndex = resultsGridView.Rows.Add(
                result.Center.X.ToString("0.0"),
                result.Center.Y.ToString("0.0"),
                result.RotationAngleDegrees.ToString("0.0"),
                result.MatchingScore.ToString("0.000"),
                result.Scale.ToString("0.00"),
                result.ShapeTransform,
                $"{result.BoundingBox.X}, {result.BoundingBox.Y}, {result.BoundingBox.Width}, {result.BoundingBox.Height}");
            resultsGridView.Rows[rowIndex].Tag = result;
        }

        resultsGridView.ClearSelection();
        if (resultsGridView.Rows.Count > 0)
        {
            resultsGridView.Rows[0].Selected = true;
        }
    }

    private void PopulateComponents(IReadOnlyList<ConnectedComponentResult> components)
    {
        componentsGridView.Rows.Clear();
        foreach (var component in components)
        {
            var box = component.BoundingBox;
            var rowIndex = componentsGridView.Rows.Add(
                component.Id.ToString(),
                component.Center.X.ToString("0.0"),
                component.Center.Y.ToString("0.0"),
                component.RotationAngleDegrees.ToString("0.0"),
                component.Score.ToString("0.000"),
                component.ShapeScore.ToString("0.000"),
                component.ShapeTransform,
                component.ResponseScore.ToString("0.000"),
                component.Scale.ToString("0.00"),
                component.AreaPixels.ToString("0.0"),
                component.AspectRatio.ToString("0.00"),
                $"{box.X}, {box.Y}, {box.Width}, {box.Height}");
            componentsGridView.Rows[rowIndex].Tag = component;
        }

        componentsGridView.ClearSelection();
        if (componentsGridView.Rows.Count > 0)
        {
            componentsGridView.Rows[0].Selected = true;
        }
    }

    private static DetectionResult ConvertComponentToDetectionResult(ConnectedComponentResult component)
    {
        return new DetectionResult
        {
            FeatureSampleId = $"component-{component.Id:0000}",
            Center = new Point2D
            {
                X = component.Center.X,
                Y = component.Center.Y
            },
            RotationAngleDegrees = component.RotationAngleDegrees,
            MatchingScore = component.Score,
            Scale = component.Scale,
            ShapeTransform = component.ShapeTransform,
            BoundingBox = new RoiRect
            {
                X = component.BoundingBox.X,
                Y = component.BoundingBox.Y,
                Width = component.BoundingBox.Width,
                Height = component.BoundingBox.Height
            }
        };
    }

    private static bool IsAcceptedMatchCandidate(
        ConnectedComponentResult component,
        DetectionSettings settings)
    {
        return component.Score >= settings.ScoreThreshold &&
            component.Scale >= settings.ScaleMin &&
            component.Scale <= settings.ScaleMax;
    }

    private void DrawOverlay(IReadOnlyList<DetectionResult> results)
    {
        if (testImageBitmap is null)
        {
            return;
        }

        using var referenceFeatureMask = isFeatureOverlayEnabled
            ? CreateReferenceFeatureMaskSnapshot()
            : null;
        var overlay = CreateOverlayBitmap(testImageBitmap, GetSelectedResults(), referenceFeatureMask);
        SetPreviewImage(overlay);
    }

    private static Bitmap CreateOverlayBitmap(
        Bitmap source,
        IReadOnlyList<DetectionResult> results,
        Mat? referenceFeatureMask = null)
    {
        var overlay = new Bitmap(source);
        using var graphics = Graphics.FromImage(overlay);
        using var boxPen = new Pen(Color.Lime, 2.0f);
        using var centerPen = new Pen(Color.Yellow, 2.0f);
        using var anglePen = new Pen(Color.DeepSkyBlue, 2.0f);
        using var labelBrush = new SolidBrush(Color.Yellow);
        using var labelBackgroundBrush = new SolidBrush(Color.FromArgb(160, Color.Black));
        var labelFont = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;

        foreach (var result in results)
        {
            var box = result.BoundingBox;
            var rectangle = new Rectangle(box.X, box.Y, box.Width, box.Height);
            DrawReferenceFeatureMask(graphics, referenceFeatureMask, rectangle);
            graphics.DrawRectangle(boxPen, rectangle);

            var centerX = (float)result.Center.X;
            var centerY = (float)result.Center.Y;
            graphics.DrawLine(centerPen, centerX - 6, centerY, centerX + 6, centerY);
            graphics.DrawLine(centerPen, centerX, centerY - 6, centerX, centerY + 6);

            var angleRadians = result.RotationAngleDegrees * Math.PI / 180.0;
            var angleLength = Math.Max(20.0f, Math.Min(rectangle.Width, rectangle.Height) / 2.0f);
            graphics.DrawLine(
                anglePen,
                centerX,
                centerY,
                centerX + (float)(Math.Cos(angleRadians) * angleLength),
                centerY + (float)(Math.Sin(angleRadians) * angleLength));

            var label = $"{result.MatchingScore:0.000}  S:{result.Scale:0.00}  {result.RotationAngleDegrees:0.0} deg";
            var labelSize = graphics.MeasureString(label, labelFont);
            var labelBounds = new RectangleF(rectangle.Left, Math.Max(0, rectangle.Top - labelSize.Height), labelSize.Width, labelSize.Height);
            graphics.FillRectangle(labelBackgroundBrush, labelBounds);
            graphics.DrawString(label, labelFont, labelBrush, labelBounds.Location);
        }

        return overlay;
    }

    private static void DrawReferenceFeatureMask(
        Graphics graphics,
        Mat? referenceFeatureMask,
        Rectangle matchRectangle)
    {
        if (referenceFeatureMask is null ||
            referenceFeatureMask.Empty() ||
            matchRectangle.Width <= 0 ||
            matchRectangle.Height <= 0)
        {
            return;
        }

        using var featureBitmap = CreateTintedFeatureMaskBitmap(
            referenceFeatureMask,
            Color.FromArgb(105, Color.DeepSkyBlue));
        if (featureBitmap is null)
        {
            return;
        }

        var scale = matchRectangle.Height / (float)Math.Max(1, featureBitmap.Height);
        var scaledWidth = featureBitmap.Width * scale;
        var destination = new RectangleF(
            matchRectangle.Left + (matchRectangle.Width - scaledWidth) / 2.0f,
            matchRectangle.Top,
            scaledWidth,
            matchRectangle.Height);

        var previousInterpolation = graphics.InterpolationMode;
        var previousPixelOffset = graphics.PixelOffsetMode;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.DrawImage(featureBitmap, destination);
        graphics.InterpolationMode = previousInterpolation;
        graphics.PixelOffsetMode = previousPixelOffset;
    }

    private static Bitmap? CreateTintedFeatureMaskBitmap(Mat featureMask, Color color)
    {
        using var binaryMask = CreateBinaryMask(featureMask);
        var foregroundRect = FindForegroundRect(binaryMask);
        if (foregroundRect.Width <= 0 || foregroundRect.Height <= 0)
        {
            return null;
        }

        using var croppedMask = new Mat(binaryMask, foregroundRect);
        var bitmap = new Bitmap(croppedMask.Width, croppedMask.Height, PixelFormat.Format32bppArgb);
        var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            var stride = Math.Abs(bitmapData.Stride);
            var pixels = new byte[stride * bitmap.Height];
            for (var y = 0; y < croppedMask.Rows; y++)
            {
                for (var x = 0; x < croppedMask.Cols; x++)
                {
                    if (croppedMask.At<byte>(y, x) == 0)
                    {
                        continue;
                    }

                    var offset = y * stride + x * 4;
                    pixels[offset] = color.B;
                    pixels[offset + 1] = color.G;
                    pixels[offset + 2] = color.R;
                    pixels[offset + 3] = color.A;
                }
            }

            Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return bitmap;
    }

    private static Mat CreateBinaryMask(Mat mask)
    {
        Mat grayscale;
        if (mask.Channels() == 1)
        {
            grayscale = mask.Clone();
        }
        else
        {
            grayscale = new Mat();
            var conversion = mask.Channels() == 4
                ? ColorConversionCodes.BGRA2GRAY
                : ColorConversionCodes.BGR2GRAY;
            Cv2.CvtColor(mask, grayscale, conversion);
        }

        var binary = new Mat();
        Cv2.Threshold(grayscale, binary, 0, 255, ThresholdTypes.Binary);
        grayscale.Dispose();
        return binary;
    }

    private static OpenCvSharp.Rect FindForegroundRect(Mat binaryMask)
    {
        Cv2.FindContours(
            binaryMask,
            out OpenCvSharp.Point[][] contours,
            out _,
            RetrievalModes.External,
            ContourApproximationModes.ApproxSimple);

        if (contours.Length == 0)
        {
            return new OpenCvSharp.Rect();
        }

        var rect = Cv2.BoundingRect(contours[0]);
        for (var index = 1; index < contours.Length; index++)
        {
            rect = Union(rect, Cv2.BoundingRect(contours[index]));
        }

        return rect;
    }

    private static OpenCvSharp.Rect Union(OpenCvSharp.Rect first, OpenCvSharp.Rect second)
    {
        var left = Math.Min(first.Left, second.Left);
        var top = Math.Min(first.Top, second.Top);
        var right = Math.Max(first.Right, second.Right);
        var bottom = Math.Max(first.Bottom, second.Bottom);
        return OpenCvSharp.Rect.FromLTRB(left, top, right, bottom);
    }

    private static Bitmap CreateComponentOverlayBitmap(
        Bitmap source,
        IReadOnlyList<ConnectedComponentResult> components)
    {
        var overlay = new Bitmap(source);
        using var graphics = Graphics.FromImage(overlay);
        using var boxPen = new Pen(Color.Orange, 2.0f);
        using var centerPen = new Pen(Color.Cyan, 2.0f);
        using var labelBrush = new SolidBrush(Color.Orange);
        using var labelBackgroundBrush = new SolidBrush(Color.FromArgb(170, Color.Black));
        var labelFont = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;

        foreach (var component in components)
        {
            var box = component.BoundingBox;
            var rectangle = new Rectangle(box.X, box.Y, box.Width, box.Height);
            graphics.DrawRectangle(boxPen, rectangle);

            var centerX = (float)component.Center.X;
            var centerY = (float)component.Center.Y;
            graphics.DrawLine(centerPen, centerX - 6, centerY, centerX + 6, centerY);
            graphics.DrawLine(centerPen, centerX, centerY - 6, centerX, centerY + 6);

            var label = $"#{component.Id} {component.Score:0.000} S:{component.Scale:0.00} {component.RotationAngleDegrees:0.0} deg";
            var labelSize = graphics.MeasureString(label, labelFont);
            var labelBounds = new RectangleF(
                rectangle.Left,
                Math.Max(0, rectangle.Top - labelSize.Height),
                labelSize.Width,
                labelSize.Height);
            graphics.FillRectangle(labelBackgroundBrush, labelBounds);
            graphics.DrawString(label, labelFont, labelBrush, labelBounds.Location);
        }

        return overlay;
    }

    private void SetPreviewImage(Image image)
    {
        var previousImage = previewPictureBox.Image;
        previewPictureBox.Image = image;
        previousImage?.Dispose();
    }

    private async Task RunCameraPreviewAsync(
        int cameraIndex,
        CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            cameraService.Open(cameraIndex, VideoCaptureAPIs.DSHOW);
            PostToUi(() => imageStatusLabel.Text = "Camera: live");

            await cameraService.StartAsync(OnCameraFrameAsync, cancellationTokenSource.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or OpenCVException)
        {
            PostToUi(() =>
            {
                MessageBox.Show(this, ex.Message, "Camera", MessageBoxButtons.OK, MessageBoxIcon.Error);
                startCameraButton.Enabled = true;
                stopCameraButton.Enabled = false;
                imageStatusLabel.Text = "Camera: error";
            });
        }
        finally
        {
            cameraService.Close();
            cancellationTokenSource.Dispose();

            PostToUi(() =>
            {
                if (ReferenceEquals(cameraCancellationTokenSource, cancellationTokenSource))
                {
                    cameraCancellationTokenSource = null;
                    startCameraButton.Enabled = true;
                    stopCameraButton.Enabled = false;
                    imageStatusLabel.Text = "Camera: stopped";
                }
            });
        }
    }

    private Task OnCameraFrameAsync(Mat frame, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if ((now - lastCameraFrameDisplayedUtc).TotalMilliseconds < 33)
        {
            return Task.CompletedTask;
        }

        lastCameraFrameDisplayedUtc = now;
        var detectionWasEnabled = isDetectionEnabled;
        var selectedResults = Array.Empty<DetectionResult>() as IReadOnlyList<DetectionResult>;
        IReadOnlyList<ConnectedComponentResult> components = Array.Empty<ConnectedComponentResult>();
        var stopwatch = new Stopwatch();
        IReadOnlyList<DetectionResult> results = Array.Empty<DetectionResult>();
        var processingText = "Processing: -";
        Bitmap bitmap;

        if (detectionWasEnabled)
        {
            ApplySettingsUiToSettings(CurrentSettings);
            stopwatch.Start();
            lock (referenceShapeLock)
            {
                components = blackHatComponentDetector
                    .Detect(frame, CurrentSettings, referenceShapeMasks, applyScaleFilter: false)
                    .OrderByDescending(component => component.Score)
                    .ToList();
                results = components
                    .Where(component => IsAcceptedMatchCandidate(component, CurrentSettings))
                    .Take(CurrentSettings.MaximumDetections)
                    .Select(ConvertComponentToDetectionResult)
                    .ToList();
            }
            stopwatch.Stop();

            selectedResults = SelectDisplayResults(results);
            using var referenceFeatureMask = isFeatureOverlayEnabled
                ? CreateReferenceFeatureMaskSnapshot()
                : null;
            bitmap = CreatePreviewBitmap(
                frame,
                selectedResults,
                Array.Empty<ConnectedComponentResult>(),
                allowOverlay: true,
                showComponentOverlay: false,
                referenceFeatureMask);
            processingText = $"Processing: {stopwatch.ElapsedMilliseconds} ms";
        }
        else
        {
            bitmap = CreatePreviewBitmap(
                frame,
                selectedResults,
                Array.Empty<ConnectedComponentResult>(),
                allowOverlay: false,
                showComponentOverlay: false,
                referenceFeatureMask: null);
        }

        var posted = false;

        try
        {
            if (!IsDisposed &&
                IsHandleCreated &&
                !cancellationToken.IsCancellationRequested &&
                Interlocked.CompareExchange(ref pendingPreviewUpdates, 1, 0) == 0)
            {
                var previewBitmap = bitmap;
                var previewResults = results;
                var previewComponents = components;
                var previewProcessingText = processingText;
                var previewDetectionWasEnabled = detectionWasEnabled;
                BeginInvoke((MethodInvoker)(() =>
                {
                    try
                    {
                        SetPreviewImage(previewBitmap);
                        if (previewDetectionWasEnabled)
                        {
                            UpdateLiveResultStatus(previewResults, previewComponents, previewProcessingText);
                        }
                        else
                        {
                            processingTimeStatusLabel.Text = "Processing: -";
                        }
                    }
                    finally
                    {
                        Interlocked.Exchange(ref pendingPreviewUpdates, 0);
                    }

                    imageStatusLabel.Text = "Camera: live";
                }));
                posted = true;
            }
        }
        catch (InvalidOperationException)
        {
            Interlocked.Exchange(ref pendingPreviewUpdates, 0);
        }

        if (!posted)
        {
            bitmap.Dispose();
        }

        return Task.CompletedTask;
    }

    private static Bitmap ConvertMatToBitmap(Mat frame)
    {
        Cv2.ImEncode(".bmp", frame, out var bytes);
        using var stream = new MemoryStream(bytes);
        using var bitmap = new Bitmap(stream);
        return new Bitmap(bitmap);
    }

    private void UpdateLiveResultStatus(
        IReadOnlyList<DetectionResult> results,
        IReadOnlyList<ConnectedComponentResult> components,
        string processingText)
    {
        lastComponents = components;
        lastStaticResults = results;
        PopulateResults(results);
        PopulateComponents(components);
        resultStatusLabel.Text = $"Count: {results.Count}";
        processingTimeStatusLabel.Text = processingText;

        if (results.Count == 0)
        {
            centerXStatusLabel.Text = "CenterX: -";
            centerYStatusLabel.Text = "CenterY: -";
            angleStatusLabel.Text = "Angle: -";
            scoreStatusLabel.Text = "Score: -";
            return;
        }

        var bestResult = results.OrderByDescending(result => result.MatchingScore).First();
        centerXStatusLabel.Text = $"CenterX: {bestResult.Center.X:0.0}";
        centerYStatusLabel.Text = $"CenterY: {bestResult.Center.Y:0.0}";
        angleStatusLabel.Text = $"Angle: {bestResult.RotationAngleDegrees:0.0}";
        scoreStatusLabel.Text = $"Score: {bestResult.MatchingScore:0.000}";
    }

    private void ClearLiveResultStatus(string processingText)
    {
        resultsGridView.Rows.Clear();
        resultStatusLabel.Text = "Count: 0";
        centerXStatusLabel.Text = "CenterX: -";
        centerYStatusLabel.Text = "CenterY: -";
        angleStatusLabel.Text = "Angle: -";
        scoreStatusLabel.Text = "Score: -";
        processingTimeStatusLabel.Text = processingText;
    }

    private void PostToUi(Action action)
    {
        if (IsDisposed)
        {
            return;
        }

        try
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)(() => action()));
                return;
            }

            action();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private DetectionSettings CurrentSettings => loadedManifest?.DetectionSettings ?? previewSettings;

    private RuntimePreviewStage CurrentPreviewStage => previewStageComboBox.SelectedIndex switch
    {
        0 => RuntimePreviewStage.Raw,
        1 => RuntimePreviewStage.Grayscale,
        2 => RuntimePreviewStage.Blurred,
        3 => RuntimePreviewStage.BlackHat,
        4 => RuntimePreviewStage.BlackHatBinary,
        5 => RuntimePreviewStage.BlackHatComponents,
        _ => RuntimePreviewStage.Overlay
    };

    private void ApplySettingsUiToSettings(DetectionSettings settings)
    {
        settings.ScoreThreshold = (double)scoreThresholdNumericUpDown.Value;
        settings.ScaleMin = (double)scaleMinNumericUpDown.Value;
        settings.ScaleMax = (double)scaleMaxNumericUpDown.Value;
        settings.ComponentShapeScoreWeight = (double)shapeWeightNumericUpDown.Value;
        settings.ComponentShapeDistanceSensitivity = (double)shapeSensitivityNumericUpDown.Value;
        settings.ComponentShapeNormalizeRotation = shapeRotationCheckBox.Checked;
        settings.ComponentShapeAllowFlip = shapeFlipCheckBox.Checked;
        settings.BlurKernelSize = (int)blurKernelNumericUpDown.Value;
        settings.BlackHatKernelSize = (int)blackHatKernelNumericUpDown.Value;
        settings.ComponentThreshold = (double)componentThresholdNumericUpDown.Value;
        settings.ComponentOpenKernelSize = (int)componentOpenNumericUpDown.Value;
        settings.ComponentCloseKernelSize = (int)componentCloseNumericUpDown.Value;
        settings.ComponentMinArea = (double)componentMinAreaNumericUpDown.Value;
        settings.ComponentMaxArea = (double)componentMaxAreaNumericUpDown.Value;
        settings.ComponentMinWidth = (int)componentMinWidthNumericUpDown.Value;
        settings.ComponentMaxWidth = (int)componentMaxWidthNumericUpDown.Value;
        settings.ComponentMinHeight = (int)componentMinHeightNumericUpDown.Value;
        settings.ComponentMaxHeight = (int)componentMaxHeightNumericUpDown.Value;
        settings.ComponentMinAspectRatio = (double)componentMinAspectNumericUpDown.Value;
        settings.ComponentMaxAspectRatio = (double)componentMaxAspectNumericUpDown.Value;
    }

    private void SyncSettingsUiFromSettings(DetectionSettings settings)
    {
        isUpdatingSettingsUi = true;
        try
        {
            scoreThresholdNumericUpDown.Value = ClampDecimal((decimal)settings.ScoreThreshold, scoreThresholdNumericUpDown.Minimum, scoreThresholdNumericUpDown.Maximum);
            scaleMinNumericUpDown.Value = ClampDecimal((decimal)settings.ScaleMin, scaleMinNumericUpDown.Minimum, scaleMinNumericUpDown.Maximum);
            scaleMaxNumericUpDown.Value = ClampDecimal((decimal)settings.ScaleMax, scaleMaxNumericUpDown.Minimum, scaleMaxNumericUpDown.Maximum);
            shapeWeightNumericUpDown.Value = ClampDecimal((decimal)settings.ComponentShapeScoreWeight, shapeWeightNumericUpDown.Minimum, shapeWeightNumericUpDown.Maximum);
            shapeSensitivityNumericUpDown.Value = ClampDecimal((decimal)settings.ComponentShapeDistanceSensitivity, shapeSensitivityNumericUpDown.Minimum, shapeSensitivityNumericUpDown.Maximum);
            shapeRotationCheckBox.Checked = settings.ComponentShapeNormalizeRotation;
            shapeFlipCheckBox.Checked = settings.ComponentShapeAllowFlip;
            blurKernelNumericUpDown.Value = ClampDecimal(settings.BlurKernelSize, blurKernelNumericUpDown.Minimum, blurKernelNumericUpDown.Maximum);
            blackHatKernelNumericUpDown.Value = ClampDecimal(settings.BlackHatKernelSize, blackHatKernelNumericUpDown.Minimum, blackHatKernelNumericUpDown.Maximum);
            componentThresholdNumericUpDown.Value = ClampDecimal((decimal)settings.ComponentThreshold, componentThresholdNumericUpDown.Minimum, componentThresholdNumericUpDown.Maximum);
            componentOpenNumericUpDown.Value = ClampDecimal(settings.ComponentOpenKernelSize, componentOpenNumericUpDown.Minimum, componentOpenNumericUpDown.Maximum);
            componentCloseNumericUpDown.Value = ClampDecimal(settings.ComponentCloseKernelSize, componentCloseNumericUpDown.Minimum, componentCloseNumericUpDown.Maximum);
            componentMinAreaNumericUpDown.Value = ClampDecimal((decimal)settings.ComponentMinArea, componentMinAreaNumericUpDown.Minimum, componentMinAreaNumericUpDown.Maximum);
            componentMaxAreaNumericUpDown.Value = ClampDecimal((decimal)settings.ComponentMaxArea, componentMaxAreaNumericUpDown.Minimum, componentMaxAreaNumericUpDown.Maximum);
            componentMinWidthNumericUpDown.Value = ClampDecimal(settings.ComponentMinWidth, componentMinWidthNumericUpDown.Minimum, componentMinWidthNumericUpDown.Maximum);
            componentMaxWidthNumericUpDown.Value = ClampDecimal(settings.ComponentMaxWidth, componentMaxWidthNumericUpDown.Minimum, componentMaxWidthNumericUpDown.Maximum);
            componentMinHeightNumericUpDown.Value = ClampDecimal(settings.ComponentMinHeight, componentMinHeightNumericUpDown.Minimum, componentMinHeightNumericUpDown.Maximum);
            componentMaxHeightNumericUpDown.Value = ClampDecimal(settings.ComponentMaxHeight, componentMaxHeightNumericUpDown.Minimum, componentMaxHeightNumericUpDown.Maximum);
            componentMinAspectNumericUpDown.Value = ClampDecimal((decimal)settings.ComponentMinAspectRatio, componentMinAspectNumericUpDown.Minimum, componentMinAspectNumericUpDown.Maximum);
            componentMaxAspectNumericUpDown.Value = ClampDecimal((decimal)settings.ComponentMaxAspectRatio, componentMaxAspectNumericUpDown.Minimum, componentMaxAspectNumericUpDown.Maximum);
        }
        finally
        {
            isUpdatingSettingsUi = false;
        }
    }

    private void RefreshStaticPreview()
    {
        if (testImageBitmap is null || string.IsNullOrWhiteSpace(testImagePath) || cameraCancellationTokenSource is not null)
        {
            return;
        }

        ApplySettingsUiToSettings(CurrentSettings);
        using var frame = Cv2.ImRead(testImagePath, ImreadModes.Color);
        using var referenceFeatureMask = isFeatureOverlayEnabled
            ? CreateReferenceFeatureMaskSnapshot()
            : null;
        var bitmap = CreatePreviewBitmap(
            frame,
            GetSelectedResults(),
            GetSelectedComponents(),
            allowOverlay: true,
            showComponentOverlay: resultsTabControl.SelectedTab == componentsTabPage,
            referenceFeatureMask);
        SetPreviewImage(bitmap);
    }

    private Bitmap CreatePreviewBitmap(
        Mat frame,
        IReadOnlyList<DetectionResult> selectedResults,
        IReadOnlyList<ConnectedComponentResult> selectedComponents,
        bool allowOverlay,
        bool showComponentOverlay,
        Mat? referenceFeatureMask)
    {
        ApplySettingsUiToSettings(CurrentSettings);
        var stage = CurrentPreviewStage;

        if (stage == RuntimePreviewStage.Overlay && allowOverlay)
        {
            using var rawBitmap = ConvertMatToBitmap(frame);
            return showComponentOverlay
                ? CreateComponentOverlayBitmap(rawBitmap, selectedComponents)
                : CreateOverlayBitmap(rawBitmap, selectedResults, referenceFeatureMask);
        }

        if (stage == RuntimePreviewStage.Raw || stage == RuntimePreviewStage.Overlay)
        {
            return ConvertMatToBitmap(frame);
        }

        using var gray = TemplateFeatureMatcher.ToGray(frame);
        if (stage == RuntimePreviewStage.Grayscale)
        {
            return ConvertMatToBitmap(gray);
        }

        if (stage == RuntimePreviewStage.Blurred)
        {
            using var blurred = TemplateFeatureMatcher.CreateBlurredGrayscale(gray, CurrentSettings);
            return ConvertMatToBitmap(blurred);
        }

        using var response = TemplateFeatureMatcher.CreateDarkFeatureResponse(gray, CurrentSettings);
        if (stage == RuntimePreviewStage.BlackHat)
        {
            return ConvertMatToBitmap(response);
        }

        using var binary = blackHatComponentDetector.CreateBinaryMask(response, CurrentSettings);
        if (stage == RuntimePreviewStage.BlackHatBinary)
        {
            return ConvertMatToBitmap(binary);
        }

        using var responseBitmap = ConvertMatToBitmap(response);
        return CreateComponentOverlayBitmap(responseBitmap, selectedComponents);
    }

    private IReadOnlyList<DetectionResult> GetSelectedResults()
    {
        if (resultsGridView.SelectedRows.Count == 0)
        {
            return Array.Empty<DetectionResult>();
        }

        return resultsGridView.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.Tag)
            .OfType<DetectionResult>()
            .ToList();
    }

    private IReadOnlyList<ConnectedComponentResult> GetSelectedComponents()
    {
        if (componentsGridView.SelectedRows.Count == 0)
        {
            return Array.Empty<ConnectedComponentResult>();
        }

        return componentsGridView.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.Tag)
            .OfType<ConnectedComponentResult>()
            .ToList();
    }

    private void UpdateSelectedComponentStatus()
    {
        var selectedComponent = GetSelectedComponents().FirstOrDefault();
        if (selectedComponent is null)
        {
            return;
        }

        centerXStatusLabel.Text = $"CenterX: {selectedComponent.Center.X:0.0}";
        centerYStatusLabel.Text = $"CenterY: {selectedComponent.Center.Y:0.0}";
        angleStatusLabel.Text = $"Angle: {selectedComponent.RotationAngleDegrees:0.0}";
        scoreStatusLabel.Text = $"Score: {selectedComponent.Score:0.000}";
    }

    private static IReadOnlyList<DetectionResult> SelectDisplayResults(IReadOnlyList<DetectionResult> results)
    {
        if (results.Count == 0)
        {
            return Array.Empty<DetectionResult>();
        }

        return new[] { results.OrderByDescending(result => result.MatchingScore).First() };
    }

    private static decimal ClampDecimal(decimal value, decimal minimum, decimal maximum)
    {
        return Math.Min(maximum, Math.Max(minimum, value));
    }

    private async Task ExtractPackageAssetsAsync(
        string packagePath,
        FeatureFileManifest manifest,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        foreach (var sample in manifest.FeatureModel.Samples)
        {
            sample.ImagePath = await ExtractEntryAsync(archive, sample.ImagePath, destinationDirectory, cancellationToken)
                .ConfigureAwait(false);
            sample.MaskPath = await ExtractEntryAsync(archive, sample.MaskPath, destinationDirectory, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async Task<string> ExtractEntryAsync(
        ZipArchive archive,
        string entryName,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        var normalizedEntryName = NormalizePackageEntryName(entryName);
        var entry = archive.GetEntry(normalizedEntryName)
            ?? throw new FileNotFoundException("A referenced feature package entry is missing.", normalizedEntryName);

        var destinationPath = Path.Combine(destinationDirectory, normalizedEntryName.Replace('/', Path.DirectorySeparatorChar));
        var destinationPathDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(destinationPathDirectory))
        {
            Directory.CreateDirectory(destinationPathDirectory);
        }

        await using var entryStream = entry.Open();
        await using var destinationStream = File.Create(destinationPath);
        await entryStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
        return destinationPath;
    }

    private static string NormalizePackageEntryName(string entryName)
    {
        if (Path.IsPathFullyQualified(entryName))
        {
            throw new InvalidDataException("Package entries must use relative paths.");
        }

        var normalized = entryName.Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrWhiteSpace(normalized) ||
            normalized.Equals("..", StringComparison.Ordinal) ||
            normalized.Contains("../", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Package entry path is invalid.");
        }

        return normalized;
    }

    private static string CreatePackageAssetDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "FeatureVision.RuntimeApp",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private void LoadReferenceShapeMasks(FeatureFileManifest manifest)
    {
        lock (referenceShapeLock)
        {
            DisposeReferenceShapeMasksNoLock();
            foreach (var sample in manifest.FeatureModel.Samples)
            {
                if (string.IsNullOrWhiteSpace(sample.MaskPath) || !File.Exists(sample.MaskPath))
                {
                    continue;
                }

                var mask = Cv2.ImRead(sample.MaskPath, ImreadModes.Grayscale);
                if (mask.Empty())
                {
                    mask.Dispose();
                    continue;
                }

                referenceShapeMasks.Add(mask);
            }
        }
    }

    private Mat? CreateReferenceFeatureMaskSnapshot()
    {
        lock (referenceShapeLock)
        {
            var referenceMask = referenceShapeMasks.FirstOrDefault(mask => !mask.Empty());
            return referenceMask?.Clone();
        }
    }

    private void DisposeReferenceShapeMasks()
    {
        lock (referenceShapeLock)
        {
            DisposeReferenceShapeMasksNoLock();
        }
    }

    private void DisposeReferenceShapeMasksNoLock()
    {
        foreach (var mask in referenceShapeMasks)
        {
            mask.Dispose();
        }

        referenceShapeMasks.Clear();
    }

    private void DisposePreviewImage()
    {
        testImageBitmap?.Dispose();
        testImageBitmap = null;

        var previewImage = previewPictureBox.Image;
        previewPictureBox.Image = null;
        previewImage?.Dispose();
    }

    private void StopCameraForShutdown()
    {
        StopCamera();

        try
        {
            cameraPreviewTask?.Wait(TimeSpan.FromMilliseconds(750));
        }
        catch (AggregateException ex) when (ex.InnerExceptions.All(inner => inner is OperationCanceledException or TaskCanceledException))
        {
        }
        catch (ObjectDisposedException)
        {
        }

        cameraService.Dispose();
    }

    private void DisposePackageAssets()
    {
        if (string.IsNullOrWhiteSpace(packageAssetDirectory) || !Directory.Exists(packageAssetDirectory))
        {
            packageAssetDirectory = null;
            return;
        }

        try
        {
            Directory.Delete(packageAssetDirectory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        packageAssetDirectory = null;
    }
}

internal enum RuntimePreviewStage
{
    Raw,
    Grayscale,
    Blurred,
    BlackHat,
    BlackHatBinary,
    BlackHatComponents,
    Overlay
}
