using FeatureVision.Core.IO;
using FeatureVision.AnnotationTool.Models;
using FeatureVision.Core.Detection;
using FeatureVision.Core.Geometry;
using FeatureVision.Core.Models;
using System.Drawing.Imaging;
using OpenCvSharp;

namespace FeatureVision.AnnotationTool.Forms;

public partial class MainForm : Form
{
    private readonly FeatureFileReader featureFileReader = new();
    private readonly FeatureFileWriter featureFileWriter = new();
    private readonly GeometryAnalyzer geometryAnalyzer = new();
    private readonly BlackHatComponentDetector blackHatComponentDetector = new();
    private readonly DetectionSettings componentSettings = new();
    private readonly List<AnnotatedImage> annotatedImages = new();
    private IReadOnlyList<ConnectedComponentResult> lastComponents = Array.Empty<ConnectedComponentResult>();
    private string? packageAssetDirectory;
    private bool isUpdatingComponentSettingsUi;

    public MainForm()
    {
        InitializeComponent();
        InitializeComponentsGrid();
        annotationCanvas.MaskChanged += AnnotationCanvas_MaskChanged;
    }

    public void OpenImages(IEnumerable<string> imagePaths)
    {
        foreach (var imagePath in imagePaths)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                continue;
            }

            var annotatedImage = new AnnotatedImage(imagePath);
            UpdateGeometry(annotatedImage);
            annotatedImages.Add(annotatedImage);
            imageListBox.Items.Add(annotatedImage);
        }

        if (imageListBox.SelectedIndex < 0 && imageListBox.Items.Count > 0)
        {
            imageListBox.SelectedIndex = 0;
        }
    }

    public void SetAnnotationTool(AnnotationToolMode toolMode)
    {
        if (toolMode == AnnotationToolMode.Polygon)
        {
            return;
        }

        annotationCanvas.ToolMode = toolMode;
        rectangleToolButton.Checked = toolMode == AnnotationToolMode.RectangleRoi;
        brushToolButton.Checked = toolMode == AnnotationToolMode.Brush;
        eraserToolButton.Checked = toolMode == AnnotationToolMode.Eraser;
        measurementBoxToolButton.Checked = toolMode == AnnotationToolMode.MeasurementBox;
    }

    private void InitializeComponentsGrid()
    {
        componentsGridView.Columns.Clear();
        componentsGridView.Columns.Add("Id", "Id");
        componentsGridView.Columns.Add("CenterX", "CenterX");
        componentsGridView.Columns.Add("CenterY", "CenterY");
        componentsGridView.Columns.Add("Angle", "Angle");
        componentsGridView.Columns.Add("Score", "Score");
        componentsGridView.Columns.Add("Area", "Area");
        componentsGridView.Columns.Add("Aspect", "H/W");
        componentsGridView.Columns.Add("BoundingBox", "BoundingBox");
        componentsGridView.MultiSelect = false;
        componentsGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    }

    public async Task SaveFeaturePackageAsync(
        string packagePath,
        CancellationToken cancellationToken = default)
    {
        if (annotatedImages.Count == 0)
        {
            throw new InvalidOperationException("Open at least one image before saving a feature package.");
        }

        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "FeatureVision.AnnotationTool",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempDirectory);

        try
        {
            var manifest = CreateManifest(tempDirectory);
            await featureFileWriter.WriteAsync(packagePath, manifest, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    public async Task LoadFeaturePackageAsync(
        string packagePath,
        CancellationToken cancellationToken = default)
    {
        var tempDirectory = CreatePackageAssetDirectory();
        var loadedImages = new List<AnnotatedImage>();
        var loadCompleted = false;

        try
        {
            var manifest = await featureFileReader
                .ReadAndExtractAssetsAsync(packagePath, tempDirectory, cancellationToken)
                .ConfigureAwait(true);

            CopyDetectionSettings(manifest.DetectionSettings, componentSettings);

            foreach (var sample in manifest.FeatureModel.Samples)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var displayName = GetSampleDisplayName(sample);
                var annotatedImage = new AnnotatedImage(sample.ImagePath, sample.MaskPath, displayName);
                UpdateGeometry(annotatedImage);
                loadedImages.Add(annotatedImage);
            }

            ReplaceAnnotatedImages(loadedImages, tempDirectory);
            loadCompleted = true;
            SyncComponentSettingsUiFromSettings(componentSettings);
            componentStatusLabel.Text = $"Package loaded: {Path.GetFileName(packagePath)}";
        }
        finally
        {
            if (!loadCompleted)
            {
                foreach (var loadedImage in loadedImages)
                {
                    loadedImage.Dispose();
                }

                TryDeleteDirectory(tempDirectory);
            }
        }
    }

    private FeatureFileManifest CreateManifest(string tempDirectory)
    {
        ApplyComponentSettingsUi();
        var manifest = new FeatureFileManifest
        {
            CreatedBy = "FeatureVision.AnnotationTool",
            DetectionSettings = CreateDetectionSettingsSnapshot(),
            FeatureModel = new FeatureModel
            {
                Id = $"model-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
                Name = "Annotated Feature"
            }
        };

        for (var index = 0; index < annotatedImages.Count; index++)
        {
            var annotatedImage = annotatedImages[index];
            var maskPath = Path.Combine(tempDirectory, $"mask-{index + 1:0000}.png");
            var geometry = UpdateGeometry(annotatedImage);
            annotatedImage.SaveMask(maskPath);

            manifest.FeatureModel.Samples.Add(new FeatureSample
            {
                Id = $"sample-{index + 1:0000}",
                Name = Path.GetFileNameWithoutExtension(annotatedImage.DisplayName),
                ImagePath = annotatedImage.ImagePath,
                MaskPath = maskPath,
                ImageSize = new Size2D
                {
                    Width = annotatedImage.Image.Width,
                    Height = annotatedImage.Image.Height
                },
                Center = geometry.Center,
                RotationAngleDegrees = geometry.RotationAngleDegrees,
                BoundingBox = geometry.BoundingBox,
                AreaPixels = geometry.Area
            });
        }

        return manifest;
    }

    private DetectionSettings CreateDetectionSettingsSnapshot()
    {
        var settings = new DetectionSettings();
        CopyDetectionSettings(componentSettings, settings);
        return settings;
    }

    private void OpenImagesButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open Images",
            Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff|All Files|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            OpenImages(dialog.FileNames);
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or OutOfMemoryException)
        {
            MessageBox.Show(this, ex.Message, "Open Images", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void LoadPackageButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Load Feature Package",
            Filter = "FeatureVision Package|*.fvfeature|All Files|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            loadPackageButton.Enabled = false;
            await LoadFeaturePackageAsync(dialog.FileName);
            MessageBox.Show(this, "Feature package loaded.", "Load Feature Package", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or OutOfMemoryException or OpenCVException)
        {
            MessageBox.Show(this, ex.Message, "Load Feature Package", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadPackageButton.Enabled = true;
        }
    }

    private async void SavePackageButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Save Feature Package",
            Filter = "FeatureVision Package|*.fvfeature|All Files|*.*",
            DefaultExt = "fvfeature",
            AddExtension = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            savePackageButton.Enabled = false;
            await SaveFeaturePackageAsync(dialog.FileName);
            MessageBox.Show(this, "Feature package saved.", "Save Feature Package", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or InvalidDataException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Save Feature Package", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            savePackageButton.Enabled = true;
        }
    }

    private void RectangleToolButton_Click(object? sender, EventArgs e)
    {
        SetAnnotationTool(AnnotationToolMode.RectangleRoi);
    }

    private void BrushToolButton_Click(object? sender, EventArgs e)
    {
        SetAnnotationTool(AnnotationToolMode.Brush);
    }

    private void EraserToolButton_Click(object? sender, EventArgs e)
    {
        SetAnnotationTool(AnnotationToolMode.Eraser);
    }

    private void MeasurementBoxToolButton_Click(object? sender, EventArgs e)
    {
        SetAnnotationTool(AnnotationToolMode.MeasurementBox);
    }

    private void FitToViewButton_Click(object? sender, EventArgs e)
    {
        annotationCanvas.FitToView();
        annotationCanvas.Invalidate();
    }

    private void FindComponentsButton_Click(object? sender, EventArgs e)
    {
        try
        {
            FindComponentsForSelectedImage();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or OpenCVException)
        {
            MessageBox.Show(this, ex.Message, "Find Components", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyComponentButton_Click(object? sender, EventArgs e)
    {
        try
        {
            ApplySelectedComponentAsMask();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or OpenCVException)
        {
            MessageBox.Show(this, ex.Message, "Apply Component", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BrushSizeNumericUpDown_ValueChanged(object? sender, EventArgs e)
    {
        if (sender is NumericUpDown numericUpDown)
        {
            annotationCanvas.BrushSize = (int)numericUpDown.Value;
        }
    }

    private void ComponentSettings_ValueChanged(object? sender, EventArgs e)
    {
        if (isUpdatingComponentSettingsUi)
        {
            return;
        }

        ApplyComponentSettingsUi();
        if (imageListBox.SelectedItem is AnnotatedImage)
        {
            try
            {
                FindComponentsForSelectedImage();
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or OpenCVException)
            {
                componentStatusLabel.Text = $"Components: {ex.Message}";
            }
        }
    }

    private void ImageListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var selectedImage = imageListBox.SelectedItem as AnnotatedImage;
        annotationCanvas.SetImage(selectedImage);
        if (selectedImage is null)
        {
            ClearGeometryStatus();
            ClearComponents();
            return;
        }

        ClearComponents();
        UpdateGeometryStatus(selectedImage.Geometry);
    }

    private void ComponentsGridView_SelectionChanged(object? sender, EventArgs e)
    {
        var component = GetSelectedComponent();
        annotationCanvas.SetComponentHighlight(component);
        if (component is null)
        {
            return;
        }

        centerXStatusLabel.Text = $"CenterX: {component.Center.X:0.0}";
        centerYStatusLabel.Text = $"CenterY: {component.Center.Y:0.0}";
        angleStatusLabel.Text = $"Angle: {component.RotationAngleDegrees:0.0}";
        areaStatusLabel.Text = $"Area: {component.AreaPixels:0}";
    }

    private void FindComponentsForSelectedImage()
    {
        var selectedImage = imageListBox.SelectedItem as AnnotatedImage
            ?? throw new InvalidOperationException("Select an image before finding components.");

        ApplyComponentSettingsUi();
        using var frame = Cv2.ImRead(selectedImage.ImagePath, ImreadModes.Color);
        if (frame.Empty())
        {
            throw new IOException("The selected image could not be loaded.");
        }

        var components = blackHatComponentDetector
            .Detect(frame, componentSettings)
            .ToList();

        lastComponents = components;
        PopulateComponents(components);
        componentStatusLabel.Text = $"Components: {components.Count}";
    }

    private void ApplySelectedComponentAsMask()
    {
        var selectedImage = imageListBox.SelectedItem as AnnotatedImage
            ?? throw new InvalidOperationException("Select an image before applying a component.");
        var selectedComponent = GetSelectedComponent()
            ?? throw new InvalidOperationException("Select a component before applying it as the mask.");

        ApplyComponentSettingsUi();
        using var frame = Cv2.ImRead(selectedImage.ImagePath, ImreadModes.Color);
        if (frame.Empty())
        {
            throw new IOException("The selected image could not be loaded.");
        }

        using var componentMask = blackHatComponentDetector.CreateComponentMask(
            frame,
            componentSettings,
            selectedComponent.Id);

        if (componentMask.Empty() || Cv2.CountNonZero(componentMask) == 0)
        {
            throw new InvalidOperationException("The selected component mask is empty.");
        }

        ApplyMaskToImage(selectedImage, componentMask);
        UpdateGeometry(selectedImage);
        annotationCanvas.SetComponentHighlight(selectedComponent);
        annotationCanvas.Invalidate();
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

    private ConnectedComponentResult? GetSelectedComponent()
    {
        if (componentsGridView.SelectedRows.Count == 0)
        {
            return null;
        }

        return componentsGridView.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.Tag)
            .OfType<ConnectedComponentResult>()
            .FirstOrDefault();
    }

    private void ClearComponents()
    {
        lastComponents = Array.Empty<ConnectedComponentResult>();
        componentsGridView.Rows.Clear();
        annotationCanvas.SetComponentHighlight(null);
        componentStatusLabel.Text = "Components: 0";
    }

    private void DisposeAnnotatedImages()
    {
        foreach (var annotatedImage in annotatedImages)
        {
            annotatedImage.Dispose();
        }

        annotatedImages.Clear();
    }

    private void ReplaceAnnotatedImages(
        IReadOnlyList<AnnotatedImage> images,
        string? assetDirectory)
    {
        annotationCanvas.SetImage(null);
        imageListBox.Items.Clear();
        ClearComponents();
        ClearGeometryStatus();
        DisposeAnnotatedImages();
        DisposePackageAssets();

        packageAssetDirectory = assetDirectory;
        foreach (var image in images)
        {
            annotatedImages.Add(image);
            imageListBox.Items.Add(image);
        }

        if (imageListBox.Items.Count > 0)
        {
            imageListBox.SelectedIndex = 0;
        }
    }

    private void AnnotationCanvas_MaskChanged(object? sender, EventArgs e)
    {
        if (imageListBox.SelectedItem is AnnotatedImage selectedImage)
        {
            UpdateGeometry(selectedImage);
        }
    }

    private GeometryResult UpdateGeometry(AnnotatedImage annotatedImage)
    {
        using var maskMat = CreateMaskMat(annotatedImage.Mask);
        annotatedImage.Geometry = geometryAnalyzer.AnalyzeMask(maskMat);

        if (ReferenceEquals(imageListBox.SelectedItem, annotatedImage))
        {
            UpdateGeometryStatus(annotatedImage.Geometry);
        }

        return annotatedImage.Geometry;
    }

    private static Mat CreateMaskMat(Bitmap mask)
    {
        using var stream = new MemoryStream();
        mask.Save(stream, ImageFormat.Png);
        return Cv2.ImDecode(stream.ToArray(), ImreadModes.Grayscale);
    }

    private void ApplyComponentSettingsUi()
    {
        componentSettings.BlurKernelSize = (int)blurKernelNumericUpDown.Value;
        componentSettings.BlackHatKernelSize = (int)blackHatKernelNumericUpDown.Value;
        componentSettings.ComponentThreshold = (double)componentThresholdNumericUpDown.Value;
        componentSettings.ComponentOpenKernelSize = (int)componentOpenNumericUpDown.Value;
        componentSettings.ComponentCloseKernelSize = (int)componentCloseNumericUpDown.Value;
        componentSettings.ComponentMinArea = (double)componentMinAreaNumericUpDown.Value;
        componentSettings.ComponentMaxArea = (double)componentMaxAreaNumericUpDown.Value;
        componentSettings.ComponentMinWidth = (int)componentMinWidthNumericUpDown.Value;
        componentSettings.ComponentMaxWidth = (int)componentMaxWidthNumericUpDown.Value;
        componentSettings.ComponentMinHeight = (int)componentMinHeightNumericUpDown.Value;
        componentSettings.ComponentMaxHeight = (int)componentMaxHeightNumericUpDown.Value;
        componentSettings.ComponentMinAspectRatio = (double)componentMinAspectNumericUpDown.Value;
        componentSettings.ComponentMaxAspectRatio = (double)componentMaxAspectNumericUpDown.Value;
    }

    private void SyncComponentSettingsUiFromSettings(DetectionSettings settings)
    {
        isUpdatingComponentSettingsUi = true;
        try
        {
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
            isUpdatingComponentSettingsUi = false;
        }

        ApplyComponentSettingsUi();
    }

    private static void ApplyMaskToImage(AnnotatedImage annotatedImage, Mat mask)
    {
        using var maskGraphics = Graphics.FromImage(annotatedImage.Mask);
        maskGraphics.Clear(Color.Black);

        Cv2.ImEncode(".bmp", mask, out var bytes);
        using var stream = new MemoryStream(bytes);
        using var maskBitmap = new Bitmap(stream);
        maskGraphics.DrawImage(maskBitmap, new Rectangle(System.Drawing.Point.Empty, annotatedImage.Mask.Size));

        using var overlayGraphics = Graphics.FromImage(annotatedImage.Overlay);
        overlayGraphics.Clear(Color.Transparent);

        for (var y = 0; y < mask.Rows; y++)
        {
            for (var x = 0; x < mask.Cols; x++)
            {
                if (mask.At<byte>(y, x) == 0)
                {
                    continue;
                }

                annotatedImage.Overlay.SetPixel(x, y, Color.FromArgb(100, Color.Red));
            }
        }
    }

    private void UpdateGeometryStatus(GeometryResult geometry)
    {
        centerXStatusLabel.Text = $"CenterX: {geometry.CenterX:0.0}";
        centerYStatusLabel.Text = $"CenterY: {geometry.CenterY:0.0}";
        angleStatusLabel.Text = $"Angle: {geometry.RotationAngleDegrees:0.0}";
        areaStatusLabel.Text = $"Area: {geometry.Area:0}";
    }

    private void ClearGeometryStatus()
    {
        centerXStatusLabel.Text = "CenterX: -";
        centerYStatusLabel.Text = "CenterY: -";
        angleStatusLabel.Text = "Angle: -";
        areaStatusLabel.Text = "Area: -";
    }

    private static string GetSampleDisplayName(FeatureSample sample)
    {
        if (!string.IsNullOrWhiteSpace(sample.Name))
        {
            return sample.Name;
        }

        if (!string.IsNullOrWhiteSpace(sample.Id))
        {
            return sample.Id;
        }

        return Path.GetFileName(sample.ImagePath);
    }

    private static void CopyDetectionSettings(
        DetectionSettings source,
        DetectionSettings target)
    {
        target.ScoreThreshold = source.ScoreThreshold;
        target.AngleMin = source.AngleMin;
        target.AngleMax = source.AngleMax;
        target.AngleStep = source.AngleStep;
        target.ScaleMin = source.ScaleMin;
        target.ScaleMax = source.ScaleMax;
        target.ScaleStep = source.ScaleStep;
        target.BlurKernelSize = source.BlurKernelSize;
        target.BlackHatKernelSize = source.BlackHatKernelSize;
        target.MaskRefinementDilateSize = source.MaskRefinementDilateSize;
        target.NmsOverlapThreshold = source.NmsOverlapThreshold;
        target.MaximumDetections = source.MaximumDetections;
        target.ComponentThreshold = source.ComponentThreshold;
        target.ComponentOpenKernelSize = source.ComponentOpenKernelSize;
        target.ComponentCloseKernelSize = source.ComponentCloseKernelSize;
        target.ComponentMinArea = source.ComponentMinArea;
        target.ComponentMaxArea = source.ComponentMaxArea;
        target.ComponentMinWidth = source.ComponentMinWidth;
        target.ComponentMaxWidth = source.ComponentMaxWidth;
        target.ComponentMinHeight = source.ComponentMinHeight;
        target.ComponentMaxHeight = source.ComponentMaxHeight;
        target.ComponentMinAspectRatio = source.ComponentMinAspectRatio;
        target.ComponentMaxAspectRatio = source.ComponentMaxAspectRatio;
        target.ComponentShapeScoreWeight = source.ComponentShapeScoreWeight;
        target.ComponentShapeProfileSamples = source.ComponentShapeProfileSamples;
        target.ComponentShapeDistanceSensitivity = source.ComponentShapeDistanceSensitivity;
        target.ComponentShapeNormalizeRotation = source.ComponentShapeNormalizeRotation;
        target.ComponentShapeAllowFlip = source.ComponentShapeAllowFlip;
        target.RegionOfInterest = source.RegionOfInterest;
    }

    private static decimal ClampDecimal(
        decimal value,
        decimal minimum,
        decimal maximum)
    {
        return Math.Min(maximum, Math.Max(minimum, value));
    }

    private static string CreatePackageAssetDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "FeatureVision.AnnotationTool",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private void DisposePackageAssets()
    {
        if (string.IsNullOrWhiteSpace(packageAssetDirectory))
        {
            return;
        }

        TryDeleteDirectory(packageAssetDirectory);
        packageAssetDirectory = null;
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
