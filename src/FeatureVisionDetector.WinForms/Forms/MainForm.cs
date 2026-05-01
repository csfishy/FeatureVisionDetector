using System.Drawing;
using System.Windows.Forms;
using FeatureVisionDetector.WinForms.Models;
using FeatureVisionDetector.WinForms.Services;
using Cv2 = OpenCvSharp.Cv2;
using ImreadModes = OpenCvSharp.ImreadModes;
using DrawingSize = System.Drawing.Size;

namespace FeatureVisionDetector.WinForms;

public sealed class MainForm : Form
{
    private const string DefaultTargetFeatureFileName = "single-line-template-expected.png";

    private readonly CameraService _cameraService = new();
    private readonly FeatureDetectionService _featureDetectionService = new();
    private readonly OverlayRenderer _overlayRenderer = new();
    private readonly DetectionSettings _detectionSettings = new();
    private readonly PictureBox _previewPictureBox;
    private readonly Button _loadSampleImageButton;
    private readonly Button _loadTargetFeatureButton;
    private readonly Button _startCameraButton;
    private readonly Button _stopCameraButton;
    private readonly CheckBox _enableDetectionCheckBox;
    private readonly Label _countLabel;
    private readonly Label _targetFeatureLabel;
    private readonly Label _statusLabel;

    private Bitmap? _currentFrame;
    private string? _loadedSampleImagePath;
    private string? _loadedTargetFeaturePath;
    private FeatureTemplate? _targetFeatureTemplate;

    public MainForm()
    {
        Text = "FeatureVisionDetector";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new DrawingSize(960, 640);
        ClientSize = new DrawingSize(1120, 720);

        _previewPictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom
        };

        _startCameraButton = new Button
        {
            AutoSize = true,
            Text = "Start Camera"
        };

        _stopCameraButton = new Button
        {
            AutoSize = true,
            Text = "Stop Camera"
        };

        _loadSampleImageButton = new Button
        {
            AutoSize = true,
            Text = "Load Sample Image"
        };

        _loadTargetFeatureButton = new Button
        {
            AutoSize = true,
            Text = "Load Target Feature"
        };

        _enableDetectionCheckBox = new CheckBox
        {
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0),
            Text = "Enable Detection"
        };

        _countLabel = new Label
        {
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0),
            Text = "Count: 0"
        };

        _targetFeatureLabel = new Label
        {
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0),
            Text = "Target feature: not selected"
        };

        _statusLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 0),
            Text = "Load a sample image to run static detection."
        };

        var commandPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(12),
            WrapContents = false
        };

        commandPanel.Controls.Add(_loadSampleImageButton);
        commandPanel.Controls.Add(_loadTargetFeatureButton);
        commandPanel.Controls.Add(_startCameraButton);
        commandPanel.Controls.Add(_stopCameraButton);
        commandPanel.Controls.Add(_enableDetectionCheckBox);
        commandPanel.Controls.Add(_countLabel);
        commandPanel.Controls.Add(_targetFeatureLabel);
        commandPanel.Controls.Add(_statusLabel);

        var layout = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 2
        };

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(commandPanel, 0, 0);
        layout.Controls.Add(_previewPictureBox, 0, 1);

        Controls.Add(layout);

        _loadSampleImageButton.Click += (_, _) => LoadSampleImage();
        _loadTargetFeatureButton.Click += (_, _) => LoadTargetFeature();
        _startCameraButton.Click += (_, _) => StartCamera();
        _stopCameraButton.Click += (_, _) => StopCamera();
        _enableDetectionCheckBox.CheckedChanged += (_, _) => RefreshDisplay();
        SizeChanged += (_, _) => RefreshPreview();

        Load += (_, _) =>
        {
            TryLoadDefaultTargetFeature();
            RefreshPreview();
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReplaceCurrentFrame(null);
            ReplaceTargetFeatureTemplate(null);

            if (_previewPictureBox.Image is not null)
            {
                _previewPictureBox.Image.Dispose();
                _previewPictureBox.Image = null;
            }

            _cameraService.Dispose();
        }

        base.Dispose(disposing);
    }

    private void StartCamera()
    {
        _loadedSampleImagePath = null;
        _cameraService.Start();
        ReplaceCurrentFrame(_cameraService.CreatePreviewFrame(GetPreviewSurfaceSize()));
        _statusLabel.Text = "Camera preview placeholder active. Live detection is not implemented yet.";
        RefreshPreview();
    }

    private void StopCamera()
    {
        _loadedSampleImagePath = null;
        _cameraService.Stop();
        ReplaceCurrentFrame(_cameraService.CreatePreviewFrame(GetPreviewSurfaceSize()));
        _statusLabel.Text = "Camera stopped.";
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        RefreshDisplay();
    }

    private void LoadSampleImage()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff|All Files|*.*",
            Title = "Select Sample Image"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        using var loadedBitmap = new Bitmap(dialog.FileName);

        _cameraService.Stop();
        _loadedSampleImagePath = dialog.FileName;
        ReplaceCurrentFrame(new Bitmap(loadedBitmap));
        _statusLabel.Text = "Sample image loaded.";
        RefreshDisplay();
    }

    private void LoadTargetFeature()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff|All Files|*.*",
            Title = "Select Target Feature"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        TryLoadTargetFeature(dialog.FileName, isDefaultTemplate: false);
        RefreshDisplay();
    }

    private bool TryLoadTargetFeature(string targetFeaturePath, bool isDefaultTemplate)
    {
        using var source = Cv2.ImRead(targetFeaturePath, ImreadModes.Color);
        if (source.Empty())
        {
            _loadedTargetFeaturePath = null;
            ReplaceTargetFeatureTemplate(null);
            _targetFeatureLabel.Text = "Target feature: failed to load";
            _statusLabel.Text = isDefaultTemplate
                ? "Default target feature was found but could not be read."
                : "Failed to read the selected target feature image.";
            return false;
        }

        var template = _featureDetectionService.TryCreateFeatureTemplate(source);
        ReplaceTargetFeatureTemplate(template);
        if (_targetFeatureTemplate is null)
        {
            _loadedTargetFeaturePath = null;
            _targetFeatureLabel.Text = "Target feature: not detected";
            _statusLabel.Text = isDefaultTemplate
                ? "Default target feature was found, but no usable feature could be extracted."
                : "Could not extract a usable target feature from the selected image.";
            return false;
        }

        _loadedTargetFeaturePath = targetFeaturePath;
        _targetFeatureLabel.Text = isDefaultTemplate
            ? $"Target feature: {Path.GetFileName(targetFeaturePath)} (default)"
            : $"Target feature: {Path.GetFileName(targetFeaturePath)}";
        _statusLabel.Text = isDefaultTemplate
            ? "Default target feature loaded. Detection will prefer this line shape."
            : "Target feature loaded. Detection will prefer this line shape.";

        return true;
    }

    private void RefreshDisplay()
    {
        var baseFrame = _currentFrame ?? _cameraService.CreatePreviewFrame(GetPreviewSurfaceSize());
        _detectionSettings.IsDetectionEnabled = _enableDetectionCheckBox.Checked;
        var results = new List<FeatureResult>();

        if (_detectionSettings.IsDetectionEnabled && !string.IsNullOrWhiteSpace(_loadedSampleImagePath))
        {
            using var source = Cv2.ImRead(_loadedSampleImagePath, ImreadModes.Color);
            if (!source.Empty())
            {
                results = _featureDetectionService.Detect(source, _detectionSettings, _targetFeatureTemplate);
                _statusLabel.Text = _targetFeatureTemplate is null
                    ? $"Detected {results.Count} candidate feature(s) in sample image."
                    : $"Detected {results.Count} matched feature(s) using the selected target feature.";
            }
            else
            {
                _statusLabel.Text = "Failed to read the selected sample image.";
            }
        }
        else if (!string.IsNullOrWhiteSpace(_loadedSampleImagePath))
        {
            _statusLabel.Text = _targetFeatureTemplate is null
                ? "Sample image loaded. Enable Detection to analyze it."
                : "Sample image and target feature loaded. Enable Detection to analyze them.";
        }

        var renderedFrame = _overlayRenderer.Render(baseFrame, results, _detectionSettings.IsDetectionEnabled);
        ReplacePreviewImage(renderedFrame);
        _countLabel.Text = $"Count: {results.Count}";
    }

    private DrawingSize GetPreviewSurfaceSize()
    {
        var width = _previewPictureBox.ClientSize.Width > 0 ? _previewPictureBox.ClientSize.Width : 960;
        var height = _previewPictureBox.ClientSize.Height > 0 ? _previewPictureBox.ClientSize.Height : 540;
        return new DrawingSize(width, height);
    }

    private void ReplaceCurrentFrame(Bitmap? nextFrame)
    {
        _currentFrame?.Dispose();
        _currentFrame = nextFrame;
    }

    private void ReplacePreviewImage(Bitmap nextFrame)
    {
        var previousImage = _previewPictureBox.Image;
        _previewPictureBox.Image = nextFrame;
        previousImage?.Dispose();
    }

    private void ReplaceTargetFeatureTemplate(FeatureTemplate? nextTemplate)
    {
        _targetFeatureTemplate?.Dispose();
        _targetFeatureTemplate = nextTemplate;
    }

    private void TryLoadDefaultTargetFeature()
    {
        if (_targetFeatureTemplate is not null)
        {
            return;
        }

        var defaultTemplatePath = FindDefaultTargetFeaturePath();
        if (string.IsNullOrWhiteSpace(defaultTemplatePath))
        {
            return;
        }

        TryLoadTargetFeature(defaultTemplatePath, isDefaultTemplate: true);
    }

    private static string? FindDefaultTargetFeaturePath()
    {
        var searchRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };

        foreach (var root in searchRoots)
        {
            var current = new DirectoryInfo(root);
            while (current is not null)
            {
                var candidate = Path.Combine(
                    current.FullName,
                    "docs",
                    "assets",
                    DefaultTargetFeatureFileName);

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }
        }

        return null;
    }
}
