namespace FeatureVision.RuntimeApp.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private ToolStrip mainToolStrip;
    private ToolStripButton loadPackageButton;
    private ToolStripButton loadTestImageButton;
    private ToolStripButton runMatchButton;
    private ToolStrip componentToolStrip;
    private ToolStripButton findComponentsButton;
    private ToolStripLabel componentThresholdToolLabel;
    private ToolStripControlHost componentThresholdHost;
    private NumericUpDown componentThresholdNumericUpDown;
    private ToolStripLabel componentOpenToolLabel;
    private ToolStripControlHost componentOpenHost;
    private NumericUpDown componentOpenNumericUpDown;
    private ToolStripLabel componentCloseToolLabel;
    private ToolStripControlHost componentCloseHost;
    private NumericUpDown componentCloseNumericUpDown;
    private ToolStripLabel componentAreaToolLabel;
    private ToolStripControlHost componentMinAreaHost;
    private NumericUpDown componentMinAreaNumericUpDown;
    private ToolStripControlHost componentMaxAreaHost;
    private NumericUpDown componentMaxAreaNumericUpDown;
    private ToolStripLabel componentWidthToolLabel;
    private ToolStripControlHost componentMinWidthHost;
    private NumericUpDown componentMinWidthNumericUpDown;
    private ToolStripControlHost componentMaxWidthHost;
    private NumericUpDown componentMaxWidthNumericUpDown;
    private ToolStripLabel componentHeightToolLabel;
    private ToolStripControlHost componentMinHeightHost;
    private NumericUpDown componentMinHeightNumericUpDown;
    private ToolStripControlHost componentMaxHeightHost;
    private NumericUpDown componentMaxHeightNumericUpDown;
    private ToolStripLabel componentAspectToolLabel;
    private ToolStripControlHost componentMinAspectHost;
    private NumericUpDown componentMinAspectNumericUpDown;
    private ToolStripControlHost componentMaxAspectHost;
    private NumericUpDown componentMaxAspectNumericUpDown;
    private ToolStripSeparator cameraToolSeparator;
    private ToolStripButton startCameraButton;
    private ToolStripButton stopCameraButton;
    private ToolStripSeparator detectionToolSeparator;
    private ToolStripControlHost enableDetectionHost;
    private CheckBox enableDetectionCheckBox;
    private ToolStripSeparator processingToolSeparator;
    private ToolStripLabel previewStageToolLabel;
    private ToolStripComboBox previewStageComboBox;
    private ToolStripControlHost showFeatureOverlayHost;
    private CheckBox showFeatureOverlayCheckBox;
    private ToolStripLabel scoreThresholdToolLabel;
    private ToolStripControlHost scoreThresholdHost;
    private NumericUpDown scoreThresholdNumericUpDown;
    private ToolStripLabel scaleMinToolLabel;
    private ToolStripControlHost scaleMinHost;
    private NumericUpDown scaleMinNumericUpDown;
    private ToolStripLabel scaleMaxToolLabel;
    private ToolStripControlHost scaleMaxHost;
    private NumericUpDown scaleMaxNumericUpDown;
    private ToolStripLabel shapeWeightToolLabel;
    private ToolStripControlHost shapeWeightHost;
    private NumericUpDown shapeWeightNumericUpDown;
    private ToolStripLabel shapeSensitivityToolLabel;
    private ToolStripControlHost shapeSensitivityHost;
    private NumericUpDown shapeSensitivityNumericUpDown;
    private ToolStripControlHost shapeRotationHost;
    private CheckBox shapeRotationCheckBox;
    private ToolStripControlHost shapeFlipHost;
    private CheckBox shapeFlipCheckBox;
    private ToolStripLabel blurKernelToolLabel;
    private ToolStripControlHost blurKernelHost;
    private NumericUpDown blurKernelNumericUpDown;
    private ToolStripLabel blackHatKernelToolLabel;
    private ToolStripControlHost blackHatKernelHost;
    private NumericUpDown blackHatKernelNumericUpDown;
    private SplitContainer mainSplitContainer;
    private PictureBox previewPictureBox;
    private TabControl resultsTabControl;
    private TabPage matchesTabPage;
    private TabPage componentsTabPage;
    private DataGridView resultsGridView;
    private DataGridView componentsGridView;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel packageStatusLabel;
    private ToolStripStatusLabel imageStatusLabel;
    private ToolStripStatusLabel resultStatusLabel;
    private ToolStripStatusLabel centerXStatusLabel;
    private ToolStripStatusLabel centerYStatusLabel;
    private ToolStripStatusLabel angleStatusLabel;
    private ToolStripStatusLabel scoreStatusLabel;
    private ToolStripStatusLabel processingTimeStatusLabel;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopCameraForShutdown();
            DisposePreviewImage();
            DisposeReferenceShapeMasks();
            DisposePackageAssets();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        mainToolStrip = new ToolStrip();
        loadPackageButton = new ToolStripButton();
        loadTestImageButton = new ToolStripButton();
        runMatchButton = new ToolStripButton();
        componentToolStrip = new ToolStrip();
        findComponentsButton = new ToolStripButton();
        componentThresholdToolLabel = new ToolStripLabel();
        componentThresholdNumericUpDown = new NumericUpDown();
        componentThresholdHost = new ToolStripControlHost(componentThresholdNumericUpDown);
        componentOpenToolLabel = new ToolStripLabel();
        componentOpenNumericUpDown = new NumericUpDown();
        componentOpenHost = new ToolStripControlHost(componentOpenNumericUpDown);
        componentCloseToolLabel = new ToolStripLabel();
        componentCloseNumericUpDown = new NumericUpDown();
        componentCloseHost = new ToolStripControlHost(componentCloseNumericUpDown);
        componentAreaToolLabel = new ToolStripLabel();
        componentMinAreaNumericUpDown = new NumericUpDown();
        componentMinAreaHost = new ToolStripControlHost(componentMinAreaNumericUpDown);
        componentMaxAreaNumericUpDown = new NumericUpDown();
        componentMaxAreaHost = new ToolStripControlHost(componentMaxAreaNumericUpDown);
        componentWidthToolLabel = new ToolStripLabel();
        componentMinWidthNumericUpDown = new NumericUpDown();
        componentMinWidthHost = new ToolStripControlHost(componentMinWidthNumericUpDown);
        componentMaxWidthNumericUpDown = new NumericUpDown();
        componentMaxWidthHost = new ToolStripControlHost(componentMaxWidthNumericUpDown);
        componentHeightToolLabel = new ToolStripLabel();
        componentMinHeightNumericUpDown = new NumericUpDown();
        componentMinHeightHost = new ToolStripControlHost(componentMinHeightNumericUpDown);
        componentMaxHeightNumericUpDown = new NumericUpDown();
        componentMaxHeightHost = new ToolStripControlHost(componentMaxHeightNumericUpDown);
        componentAspectToolLabel = new ToolStripLabel();
        componentMinAspectNumericUpDown = new NumericUpDown();
        componentMinAspectHost = new ToolStripControlHost(componentMinAspectNumericUpDown);
        componentMaxAspectNumericUpDown = new NumericUpDown();
        componentMaxAspectHost = new ToolStripControlHost(componentMaxAspectNumericUpDown);
        cameraToolSeparator = new ToolStripSeparator();
        startCameraButton = new ToolStripButton();
        stopCameraButton = new ToolStripButton();
        detectionToolSeparator = new ToolStripSeparator();
        enableDetectionCheckBox = new CheckBox();
        enableDetectionHost = new ToolStripControlHost(enableDetectionCheckBox);
        processingToolSeparator = new ToolStripSeparator();
        previewStageToolLabel = new ToolStripLabel();
        previewStageComboBox = new ToolStripComboBox();
        showFeatureOverlayCheckBox = new CheckBox();
        showFeatureOverlayHost = new ToolStripControlHost(showFeatureOverlayCheckBox);
        scoreThresholdToolLabel = new ToolStripLabel();
        scoreThresholdNumericUpDown = new NumericUpDown();
        scoreThresholdHost = new ToolStripControlHost(scoreThresholdNumericUpDown);
        scaleMinToolLabel = new ToolStripLabel();
        scaleMinNumericUpDown = new NumericUpDown();
        scaleMinHost = new ToolStripControlHost(scaleMinNumericUpDown);
        scaleMaxToolLabel = new ToolStripLabel();
        scaleMaxNumericUpDown = new NumericUpDown();
        scaleMaxHost = new ToolStripControlHost(scaleMaxNumericUpDown);
        shapeWeightToolLabel = new ToolStripLabel();
        shapeWeightNumericUpDown = new NumericUpDown();
        shapeWeightHost = new ToolStripControlHost(shapeWeightNumericUpDown);
        shapeSensitivityToolLabel = new ToolStripLabel();
        shapeSensitivityNumericUpDown = new NumericUpDown();
        shapeSensitivityHost = new ToolStripControlHost(shapeSensitivityNumericUpDown);
        shapeRotationCheckBox = new CheckBox();
        shapeRotationHost = new ToolStripControlHost(shapeRotationCheckBox);
        shapeFlipCheckBox = new CheckBox();
        shapeFlipHost = new ToolStripControlHost(shapeFlipCheckBox);
        blurKernelToolLabel = new ToolStripLabel();
        blurKernelNumericUpDown = new NumericUpDown();
        blurKernelHost = new ToolStripControlHost(blurKernelNumericUpDown);
        blackHatKernelToolLabel = new ToolStripLabel();
        blackHatKernelNumericUpDown = new NumericUpDown();
        blackHatKernelHost = new ToolStripControlHost(blackHatKernelNumericUpDown);
        mainSplitContainer = new SplitContainer();
        previewPictureBox = new PictureBox();
        resultsTabControl = new TabControl();
        matchesTabPage = new TabPage();
        componentsTabPage = new TabPage();
        resultsGridView = new DataGridView();
        componentsGridView = new DataGridView();
        statusStrip = new StatusStrip();
        packageStatusLabel = new ToolStripStatusLabel();
        imageStatusLabel = new ToolStripStatusLabel();
        resultStatusLabel = new ToolStripStatusLabel();
        centerXStatusLabel = new ToolStripStatusLabel();
        centerYStatusLabel = new ToolStripStatusLabel();
        angleStatusLabel = new ToolStripStatusLabel();
        scoreStatusLabel = new ToolStripStatusLabel();
        processingTimeStatusLabel = new ToolStripStatusLabel();
        mainToolStrip.SuspendLayout();
        componentToolStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
        ((System.ComponentModel.ISupportInitialize)componentThresholdNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)componentOpenNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)componentCloseNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)componentMinAreaNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)componentMaxAreaNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)componentMinWidthNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)componentMaxWidthNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)componentMinHeightNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)componentMaxHeightNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)componentMinAspectNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)componentMaxAspectNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)scoreThresholdNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)scaleMinNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)scaleMaxNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)shapeWeightNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)shapeSensitivityNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)blurKernelNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)blackHatKernelNumericUpDown).BeginInit();
        mainSplitContainer.Panel1.SuspendLayout();
        mainSplitContainer.Panel2.SuspendLayout();
        mainSplitContainer.SuspendLayout();
        resultsTabControl.SuspendLayout();
        matchesTabPage.SuspendLayout();
        componentsTabPage.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)previewPictureBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)resultsGridView).BeginInit();
        ((System.ComponentModel.ISupportInitialize)componentsGridView).BeginInit();
        statusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // mainToolStrip
        // 
        mainToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        mainToolStrip.Items.AddRange(new ToolStripItem[] {
            loadPackageButton,
            loadTestImageButton,
            runMatchButton,
            cameraToolSeparator,
            startCameraButton,
            stopCameraButton,
            detectionToolSeparator,
            enableDetectionHost,
            processingToolSeparator,
            previewStageToolLabel,
            previewStageComboBox,
            showFeatureOverlayHost,
            scoreThresholdToolLabel,
            scoreThresholdHost,
            scaleMinToolLabel,
            scaleMinHost,
            scaleMaxToolLabel,
            scaleMaxHost,
            shapeWeightToolLabel,
            shapeWeightHost,
            shapeSensitivityToolLabel,
            shapeSensitivityHost,
            shapeRotationHost,
            shapeFlipHost,
            blurKernelToolLabel,
            blurKernelHost,
            blackHatKernelToolLabel,
            blackHatKernelHost});
        mainToolStrip.Location = new Point(0, 0);
        mainToolStrip.Name = "mainToolStrip";
        mainToolStrip.Size = new Size(1100, 25);
        mainToolStrip.TabIndex = 0;
        // 
        // loadPackageButton
        // 
        loadPackageButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        loadPackageButton.Text = "Load Package";
        loadPackageButton.Click += LoadPackageButton_Click;
        // 
        // loadTestImageButton
        // 
        loadTestImageButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        loadTestImageButton.Text = "Load Test Image";
        loadTestImageButton.Click += LoadTestImageButton_Click;
        // 
        // runMatchButton
        // 
        runMatchButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        runMatchButton.Text = "Run Match";
        runMatchButton.Click += RunMatchButton_Click;
        // 
        // componentToolStrip
        // 
        componentToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        componentToolStrip.Items.AddRange(new ToolStripItem[] {
            findComponentsButton,
            componentThresholdToolLabel,
            componentThresholdHost,
            componentOpenToolLabel,
            componentOpenHost,
            componentCloseToolLabel,
            componentCloseHost,
            componentAreaToolLabel,
            componentMinAreaHost,
            componentMaxAreaHost,
            componentWidthToolLabel,
            componentMinWidthHost,
            componentMaxWidthHost,
            componentHeightToolLabel,
            componentMinHeightHost,
            componentMaxHeightHost,
            componentAspectToolLabel,
            componentMinAspectHost,
            componentMaxAspectHost});
        componentToolStrip.Location = new Point(0, 25);
        componentToolStrip.Name = "componentToolStrip";
        componentToolStrip.Size = new Size(1100, 25);
        componentToolStrip.TabIndex = 1;
        // 
        // findComponentsButton
        // 
        findComponentsButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        findComponentsButton.Text = "Find Components";
        findComponentsButton.Click += FindComponentsButton_Click;
        // 
        // componentThresholdNumericUpDown
        // 
        componentThresholdToolLabel.Text = "Thr";
        componentThresholdNumericUpDown.DecimalPlaces = 0;
        componentThresholdNumericUpDown.Minimum = 0;
        componentThresholdNumericUpDown.Maximum = 255;
        componentThresholdNumericUpDown.Value = 40;
        componentThresholdNumericUpDown.Width = 55;
        componentThresholdNumericUpDown.ValueChanged += ComponentSettings_ValueChanged;
        // 
        // componentOpenNumericUpDown
        // 
        componentOpenToolLabel.Text = "Open";
        componentOpenNumericUpDown.Minimum = 1;
        componentOpenNumericUpDown.Maximum = 31;
        componentOpenNumericUpDown.Value = 1;
        componentOpenNumericUpDown.Width = 45;
        componentOpenNumericUpDown.ValueChanged += ComponentSettings_ValueChanged;
        // 
        // componentCloseNumericUpDown
        // 
        componentCloseToolLabel.Text = "Close";
        componentCloseNumericUpDown.Minimum = 1;
        componentCloseNumericUpDown.Maximum = 31;
        componentCloseNumericUpDown.Value = 3;
        componentCloseNumericUpDown.Width = 45;
        componentCloseNumericUpDown.ValueChanged += ComponentSettings_ValueChanged;
        // 
        // componentMinAreaNumericUpDown
        // 
        componentAreaToolLabel.Text = "Area";
        componentMinAreaNumericUpDown.DecimalPlaces = 0;
        componentMinAreaNumericUpDown.Minimum = 0;
        componentMinAreaNumericUpDown.Maximum = 1000000;
        componentMinAreaNumericUpDown.Value = 20;
        componentMinAreaNumericUpDown.Width = 65;
        componentMinAreaNumericUpDown.ValueChanged += ComponentSettings_ValueChanged;
        // 
        // componentMaxAreaNumericUpDown
        // 
        componentMaxAreaNumericUpDown.DecimalPlaces = 0;
        componentMaxAreaNumericUpDown.Minimum = 1;
        componentMaxAreaNumericUpDown.Maximum = 1000000;
        componentMaxAreaNumericUpDown.Value = 100000;
        componentMaxAreaNumericUpDown.Width = 75;
        componentMaxAreaNumericUpDown.ValueChanged += ComponentSettings_ValueChanged;
        // 
        // componentMinWidthNumericUpDown
        // 
        componentWidthToolLabel.Text = "W";
        componentMinWidthNumericUpDown.Minimum = 1;
        componentMinWidthNumericUpDown.Maximum = 10000;
        componentMinWidthNumericUpDown.Value = 1;
        componentMinWidthNumericUpDown.Width = 50;
        componentMinWidthNumericUpDown.ValueChanged += ComponentSettings_ValueChanged;
        // 
        // componentMaxWidthNumericUpDown
        // 
        componentMaxWidthNumericUpDown.Minimum = 1;
        componentMaxWidthNumericUpDown.Maximum = 10000;
        componentMaxWidthNumericUpDown.Value = 10000;
        componentMaxWidthNumericUpDown.Width = 60;
        componentMaxWidthNumericUpDown.ValueChanged += ComponentSettings_ValueChanged;
        // 
        // componentMinHeightNumericUpDown
        // 
        componentHeightToolLabel.Text = "H";
        componentMinHeightNumericUpDown.Minimum = 1;
        componentMinHeightNumericUpDown.Maximum = 10000;
        componentMinHeightNumericUpDown.Value = 5;
        componentMinHeightNumericUpDown.Width = 50;
        componentMinHeightNumericUpDown.ValueChanged += ComponentSettings_ValueChanged;
        // 
        // componentMaxHeightNumericUpDown
        // 
        componentMaxHeightNumericUpDown.Minimum = 1;
        componentMaxHeightNumericUpDown.Maximum = 10000;
        componentMaxHeightNumericUpDown.Value = 10000;
        componentMaxHeightNumericUpDown.Width = 60;
        componentMaxHeightNumericUpDown.ValueChanged += ComponentSettings_ValueChanged;
        // 
        // componentMinAspectNumericUpDown
        // 
        componentAspectToolLabel.Text = "Aspect";
        componentMinAspectNumericUpDown.DecimalPlaces = 1;
        componentMinAspectNumericUpDown.Increment = 0.5M;
        componentMinAspectNumericUpDown.Minimum = 0;
        componentMinAspectNumericUpDown.Maximum = 1000;
        componentMinAspectNumericUpDown.Value = 1.5M;
        componentMinAspectNumericUpDown.Width = 55;
        componentMinAspectNumericUpDown.ValueChanged += ComponentSettings_ValueChanged;
        // 
        // componentMaxAspectNumericUpDown
        // 
        componentMaxAspectNumericUpDown.DecimalPlaces = 1;
        componentMaxAspectNumericUpDown.Increment = 0.5M;
        componentMaxAspectNumericUpDown.Minimum = 0;
        componentMaxAspectNumericUpDown.Maximum = 1000;
        componentMaxAspectNumericUpDown.Value = 1000;
        componentMaxAspectNumericUpDown.Width = 65;
        componentMaxAspectNumericUpDown.ValueChanged += ComponentSettings_ValueChanged;
        // 
        // startCameraButton
        // 
        startCameraButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        startCameraButton.Text = "Start Camera";
        startCameraButton.Click += StartCameraButton_Click;
        // 
        // stopCameraButton
        // 
        stopCameraButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        stopCameraButton.Enabled = false;
        stopCameraButton.Text = "Stop Camera";
        stopCameraButton.Click += StopCameraButton_Click;
        // 
        // enableDetectionCheckBox
        // 
        enableDetectionCheckBox.AutoSize = true;
        enableDetectionCheckBox.Text = "Enable Detection";
        enableDetectionCheckBox.CheckedChanged += EnableDetectionCheckBox_CheckedChanged;
        // 
        // enableDetectionHost
        // 
        enableDetectionHost.Name = "enableDetectionHost";
        enableDetectionHost.Size = new Size(116, 22);
        // 
        // previewStageToolLabel
        // 
        previewStageToolLabel.Text = "View";
        // 
        // previewStageComboBox
        // 
        previewStageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        previewStageComboBox.Items.AddRange(new object[] {
            "Raw",
            "Grayscale",
            "Blurred",
            "BlackHat",
            "BH Binary",
            "BH Components",
            "Overlay"});
        previewStageComboBox.Name = "previewStageComboBox";
        previewStageComboBox.Size = new Size(120, 25);
        previewStageComboBox.SelectedIndex = 6;
        previewStageComboBox.SelectedIndexChanged += PreviewStageComboBox_SelectedIndexChanged;
        // 
        // showFeatureOverlayCheckBox
        // 
        showFeatureOverlayCheckBox.AutoSize = true;
        showFeatureOverlayCheckBox.Text = "Feature Overlay";
        showFeatureOverlayCheckBox.CheckedChanged += ShowFeatureOverlayCheckBox_CheckedChanged;
        // 
        // showFeatureOverlayHost
        // 
        showFeatureOverlayHost.Name = "showFeatureOverlayHost";
        showFeatureOverlayHost.Size = new Size(113, 22);
        // 
        // scoreThresholdNumericUpDown
        // 
        scoreThresholdToolLabel.Text = "Score";
        scoreThresholdNumericUpDown.DecimalPlaces = 2;
        scoreThresholdNumericUpDown.Increment = 0.05M;
        scoreThresholdNumericUpDown.Maximum = 1;
        scoreThresholdNumericUpDown.Minimum = 0;
        scoreThresholdNumericUpDown.Value = 0.70M;
        scoreThresholdNumericUpDown.Width = 55;
        scoreThresholdNumericUpDown.ValueChanged += ProcessingSettings_ValueChanged;
        // 
        // scaleMinNumericUpDown
        // 
        scaleMinToolLabel.Text = "Smin";
        scaleMinNumericUpDown.DecimalPlaces = 2;
        scaleMinNumericUpDown.Increment = 0.05M;
        scaleMinNumericUpDown.Minimum = 0.10M;
        scaleMinNumericUpDown.Maximum = 5;
        scaleMinNumericUpDown.Value = 0.90M;
        scaleMinNumericUpDown.Width = 55;
        scaleMinNumericUpDown.ValueChanged += ProcessingSettings_ValueChanged;
        // 
        // scaleMaxNumericUpDown
        // 
        scaleMaxToolLabel.Text = "Smax";
        scaleMaxNumericUpDown.DecimalPlaces = 2;
        scaleMaxNumericUpDown.Increment = 0.05M;
        scaleMaxNumericUpDown.Minimum = 0.10M;
        scaleMaxNumericUpDown.Maximum = 5;
        scaleMaxNumericUpDown.Value = 1.10M;
        scaleMaxNumericUpDown.Width = 55;
        scaleMaxNumericUpDown.ValueChanged += ProcessingSettings_ValueChanged;
        // 
        // shapeWeightNumericUpDown
        // 
        shapeWeightToolLabel.Text = "ShapeW";
        shapeWeightNumericUpDown.DecimalPlaces = 2;
        shapeWeightNumericUpDown.Increment = 0.05M;
        shapeWeightNumericUpDown.Minimum = 0;
        shapeWeightNumericUpDown.Maximum = 1;
        shapeWeightNumericUpDown.Value = 0.85M;
        shapeWeightNumericUpDown.Width = 55;
        shapeWeightNumericUpDown.ValueChanged += ProcessingSettings_ValueChanged;
        // 
        // shapeSensitivityNumericUpDown
        // 
        shapeSensitivityToolLabel.Text = "ShapeSens";
        shapeSensitivityNumericUpDown.DecimalPlaces = 1;
        shapeSensitivityNumericUpDown.Increment = 0.5M;
        shapeSensitivityNumericUpDown.Minimum = 0.1M;
        shapeSensitivityNumericUpDown.Maximum = 100;
        shapeSensitivityNumericUpDown.Value = 16.0M;
        shapeSensitivityNumericUpDown.Width = 60;
        shapeSensitivityNumericUpDown.ValueChanged += ProcessingSettings_ValueChanged;
        // 
        // shapeRotationCheckBox
        // 
        shapeRotationCheckBox.AutoSize = true;
        shapeRotationCheckBox.Checked = true;
        shapeRotationCheckBox.CheckState = CheckState.Checked;
        shapeRotationCheckBox.Text = "Rot";
        shapeRotationCheckBox.CheckedChanged += ProcessingSettings_ValueChanged;
        // 
        // shapeFlipCheckBox
        // 
        shapeFlipCheckBox.AutoSize = true;
        shapeFlipCheckBox.Checked = true;
        shapeFlipCheckBox.CheckState = CheckState.Checked;
        shapeFlipCheckBox.Text = "Flip";
        shapeFlipCheckBox.CheckedChanged += ProcessingSettings_ValueChanged;
        // 
        // blurKernelNumericUpDown
        // 
        blurKernelToolLabel.Text = "Blur";
        blurKernelNumericUpDown.Minimum = 1;
        blurKernelNumericUpDown.Maximum = 31;
        blurKernelNumericUpDown.Value = 3;
        blurKernelNumericUpDown.Width = 45;
        blurKernelNumericUpDown.ValueChanged += ProcessingSettings_ValueChanged;
        // 
        // blackHatKernelNumericUpDown
        // 
        blackHatKernelToolLabel.Text = "BH";
        blackHatKernelNumericUpDown.Minimum = 3;
        blackHatKernelNumericUpDown.Maximum = 101;
        blackHatKernelNumericUpDown.Value = 11;
        blackHatKernelNumericUpDown.Width = 45;
        blackHatKernelNumericUpDown.ValueChanged += ProcessingSettings_ValueChanged;
        // 
        // mainSplitContainer
        // 
        mainSplitContainer.Dock = DockStyle.Fill;
        mainSplitContainer.Location = new Point(0, 50);
        mainSplitContainer.Name = "mainSplitContainer";
        mainSplitContainer.Orientation = Orientation.Horizontal;
        // 
        // mainSplitContainer.Panel1
        // 
        mainSplitContainer.Panel1.Controls.Add(previewPictureBox);
        // 
        // mainSplitContainer.Panel2
        // 
        mainSplitContainer.Panel2.Controls.Add(resultsTabControl);
        mainSplitContainer.Size = new Size(1100, 678);
        mainSplitContainer.SplitterDistance = 495;
        mainSplitContainer.TabIndex = 2;
        // 
        // previewPictureBox
        // 
        previewPictureBox.BackColor = Color.FromArgb(32, 32, 32);
        previewPictureBox.Dock = DockStyle.Fill;
        previewPictureBox.Location = new Point(0, 0);
        previewPictureBox.Name = "previewPictureBox";
        previewPictureBox.Size = new Size(1100, 495);
        previewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        previewPictureBox.TabIndex = 0;
        previewPictureBox.TabStop = false;
        // 
        // resultsTabControl
        // 
        resultsTabControl.Controls.Add(matchesTabPage);
        resultsTabControl.Controls.Add(componentsTabPage);
        resultsTabControl.Dock = DockStyle.Fill;
        resultsTabControl.Location = new Point(0, 0);
        resultsTabControl.Name = "resultsTabControl";
        resultsTabControl.SelectedIndex = 0;
        resultsTabControl.Size = new Size(1100, 179);
        resultsTabControl.TabIndex = 0;
        resultsTabControl.SelectedIndexChanged += ResultsTabControl_SelectedIndexChanged;
        // 
        // matchesTabPage
        // 
        matchesTabPage.Controls.Add(resultsGridView);
        matchesTabPage.Location = new Point(4, 24);
        matchesTabPage.Name = "matchesTabPage";
        matchesTabPage.Padding = new Padding(3);
        matchesTabPage.Size = new Size(1092, 151);
        matchesTabPage.TabIndex = 0;
        matchesTabPage.Text = "Matches";
        matchesTabPage.UseVisualStyleBackColor = true;
        // 
        // componentsTabPage
        // 
        componentsTabPage.Controls.Add(componentsGridView);
        componentsTabPage.Location = new Point(4, 24);
        componentsTabPage.Name = "componentsTabPage";
        componentsTabPage.Padding = new Padding(3);
        componentsTabPage.Size = new Size(1092, 151);
        componentsTabPage.TabIndex = 1;
        componentsTabPage.Text = "BlackHat Components";
        componentsTabPage.UseVisualStyleBackColor = true;
        // 
        // resultsGridView
        // 
        resultsGridView.AllowUserToAddRows = false;
        resultsGridView.AllowUserToDeleteRows = false;
        resultsGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        resultsGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        resultsGridView.Dock = DockStyle.Fill;
        resultsGridView.Location = new Point(3, 3);
        resultsGridView.Name = "resultsGridView";
        resultsGridView.ReadOnly = true;
        resultsGridView.RowHeadersVisible = false;
        resultsGridView.Size = new Size(1086, 145);
        resultsGridView.TabIndex = 0;
        resultsGridView.SelectionChanged += ResultsGridView_SelectionChanged;
        // 
        // componentsGridView
        // 
        componentsGridView.AllowUserToAddRows = false;
        componentsGridView.AllowUserToDeleteRows = false;
        componentsGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        componentsGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        componentsGridView.Dock = DockStyle.Fill;
        componentsGridView.Location = new Point(3, 3);
        componentsGridView.Name = "componentsGridView";
        componentsGridView.ReadOnly = true;
        componentsGridView.RowHeadersVisible = false;
        componentsGridView.Size = new Size(1086, 145);
        componentsGridView.TabIndex = 0;
        componentsGridView.SelectionChanged += ComponentsGridView_SelectionChanged;
        // 
        // statusStrip
        // 
        statusStrip.Items.AddRange(new ToolStripItem[] {
            packageStatusLabel,
            imageStatusLabel,
            resultStatusLabel,
            centerXStatusLabel,
            centerYStatusLabel,
            angleStatusLabel,
            scoreStatusLabel,
            processingTimeStatusLabel});
        statusStrip.Location = new Point(0, 728);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(1100, 22);
        statusStrip.TabIndex = 2;
        // 
        // packageStatusLabel
        // 
        packageStatusLabel.Text = "Package: none";
        // 
        // imageStatusLabel
        // 
        imageStatusLabel.Text = "Image: none";
        // 
        // resultStatusLabel
        // 
        resultStatusLabel.Text = "Results: 0";
        // 
        // centerXStatusLabel
        // 
        centerXStatusLabel.Text = "CenterX: -";
        // 
        // centerYStatusLabel
        // 
        centerYStatusLabel.Text = "CenterY: -";
        // 
        // angleStatusLabel
        // 
        angleStatusLabel.Text = "Angle: -";
        // 
        // scoreStatusLabel
        // 
        scoreStatusLabel.Text = "Score: -";
        // 
        // processingTimeStatusLabel
        // 
        processingTimeStatusLabel.Text = "Processing: -";
        // 
        // MainForm
        // 
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 750);
        Controls.Add(mainSplitContainer);
        Controls.Add(statusStrip);
        Controls.Add(componentToolStrip);
        Controls.Add(mainToolStrip);
        MinimumSize = new Size(800, 500);
        Text = "FeatureVision Runtime App";
        mainToolStrip.ResumeLayout(false);
        mainToolStrip.PerformLayout();
        componentToolStrip.ResumeLayout(false);
        componentToolStrip.PerformLayout();
        mainSplitContainer.Panel1.ResumeLayout(false);
        mainSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)mainSplitContainer).EndInit();
        mainSplitContainer.ResumeLayout(false);
        resultsTabControl.ResumeLayout(false);
        matchesTabPage.ResumeLayout(false);
        componentsTabPage.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)previewPictureBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)resultsGridView).EndInit();
        ((System.ComponentModel.ISupportInitialize)componentsGridView).EndInit();
        ((System.ComponentModel.ISupportInitialize)scoreThresholdNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)scaleMinNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)scaleMaxNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)shapeWeightNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)shapeSensitivityNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)blurKernelNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)blackHatKernelNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)componentThresholdNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)componentOpenNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)componentCloseNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)componentMinAreaNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)componentMaxAreaNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)componentMinWidthNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)componentMaxWidthNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)componentMinHeightNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)componentMaxHeightNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)componentMinAspectNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)componentMaxAspectNumericUpDown).EndInit();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
