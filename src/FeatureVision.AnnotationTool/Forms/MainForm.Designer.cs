namespace FeatureVision.AnnotationTool.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private ToolStrip mainToolStrip;
    private ToolStripButton openImagesButton;
    private ToolStripButton loadPackageButton;
    private ToolStripButton savePackageButton;
    private ToolStrip componentToolStrip;
    private ToolStripButton findComponentsButton;
    private ToolStripButton applyComponentButton;
    private ToolStripLabel blurKernelToolLabel;
    private ToolStripControlHost blurKernelHost;
    private NumericUpDown blurKernelNumericUpDown;
    private ToolStripLabel blackHatKernelToolLabel;
    private ToolStripControlHost blackHatKernelHost;
    private NumericUpDown blackHatKernelNumericUpDown;
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
    private ToolStripSeparator fileToolSeparator;
    private ToolStripButton rectangleToolButton;
    private ToolStripButton brushToolButton;
    private ToolStripButton eraserToolButton;
    private ToolStripButton measurementBoxToolButton;
    private ToolStripSeparator toolSeparator;
    private ToolStripLabel brushSizeLabel;
    private ToolStripControlHost brushSizeHost;
    private ToolStripButton fitToViewButton;
    private SplitContainer mainSplitContainer;
    private SplitContainer sideSplitContainer;
    private ListBox imageListBox;
    private DataGridView componentsGridView;
    private Controls.ImageAnnotationCanvas annotationCanvas;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel centerXStatusLabel;
    private ToolStripStatusLabel centerYStatusLabel;
    private ToolStripStatusLabel angleStatusLabel;
    private ToolStripStatusLabel areaStatusLabel;
    private ToolStripStatusLabel componentStatusLabel;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeAnnotatedImages();
            DisposePackageAssets();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        mainToolStrip = new ToolStrip();
        openImagesButton = new ToolStripButton();
        loadPackageButton = new ToolStripButton();
        savePackageButton = new ToolStripButton();
        componentToolStrip = new ToolStrip();
        findComponentsButton = new ToolStripButton();
        applyComponentButton = new ToolStripButton();
        blurKernelToolLabel = new ToolStripLabel();
        blurKernelNumericUpDown = new NumericUpDown();
        blurKernelHost = new ToolStripControlHost(blurKernelNumericUpDown);
        blackHatKernelToolLabel = new ToolStripLabel();
        blackHatKernelNumericUpDown = new NumericUpDown();
        blackHatKernelHost = new ToolStripControlHost(blackHatKernelNumericUpDown);
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
        fileToolSeparator = new ToolStripSeparator();
        rectangleToolButton = new ToolStripButton();
        brushToolButton = new ToolStripButton();
        eraserToolButton = new ToolStripButton();
        measurementBoxToolButton = new ToolStripButton();
        toolSeparator = new ToolStripSeparator();
        brushSizeLabel = new ToolStripLabel();
        brushSizeHost = new ToolStripControlHost(new NumericUpDown());
        fitToViewButton = new ToolStripButton();
        mainSplitContainer = new SplitContainer();
        sideSplitContainer = new SplitContainer();
        imageListBox = new ListBox();
        componentsGridView = new DataGridView();
        annotationCanvas = new Controls.ImageAnnotationCanvas();
        statusStrip = new StatusStrip();
        centerXStatusLabel = new ToolStripStatusLabel();
        centerYStatusLabel = new ToolStripStatusLabel();
        angleStatusLabel = new ToolStripStatusLabel();
        areaStatusLabel = new ToolStripStatusLabel();
        componentStatusLabel = new ToolStripStatusLabel();
        mainToolStrip.SuspendLayout();
        componentToolStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
        ((System.ComponentModel.ISupportInitialize)sideSplitContainer).BeginInit();
        mainSplitContainer.Panel1.SuspendLayout();
        mainSplitContainer.Panel2.SuspendLayout();
        mainSplitContainer.SuspendLayout();
        sideSplitContainer.Panel1.SuspendLayout();
        sideSplitContainer.Panel2.SuspendLayout();
        sideSplitContainer.SuspendLayout();
        statusStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)brushSizeHost.Control).BeginInit();
        ((System.ComponentModel.ISupportInitialize)blurKernelNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)blackHatKernelNumericUpDown).BeginInit();
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
        ((System.ComponentModel.ISupportInitialize)componentsGridView).BeginInit();
        SuspendLayout();
        // 
        // mainToolStrip
        // 
        mainToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        mainToolStrip.Items.AddRange(new ToolStripItem[] {
            openImagesButton,
            loadPackageButton,
            savePackageButton,
            fileToolSeparator,
            rectangleToolButton,
            brushToolButton,
            eraserToolButton,
            measurementBoxToolButton,
            toolSeparator,
            brushSizeLabel,
            brushSizeHost,
            fitToViewButton});
        mainToolStrip.Location = new Point(0, 0);
        mainToolStrip.Name = "mainToolStrip";
        mainToolStrip.Size = new Size(1100, 27);
        mainToolStrip.TabIndex = 0;
        // 
        // openImagesButton
        // 
        openImagesButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        openImagesButton.Text = "Open Images";
        openImagesButton.Click += OpenImagesButton_Click;
        // 
        // loadPackageButton
        // 
        loadPackageButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        loadPackageButton.Text = "Load Package";
        loadPackageButton.Click += LoadPackageButton_Click;
        // 
        // savePackageButton
        // 
        savePackageButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        savePackageButton.Text = "Save Feature Package";
        savePackageButton.Click += SavePackageButton_Click;
        // 
        // componentToolStrip
        // 
        componentToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        componentToolStrip.Items.AddRange(new ToolStripItem[] {
            findComponentsButton,
            applyComponentButton,
            blurKernelToolLabel,
            blurKernelHost,
            blackHatKernelToolLabel,
            blackHatKernelHost,
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
        componentToolStrip.Location = new Point(0, 27);
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
        // applyComponentButton
        // 
        applyComponentButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        applyComponentButton.Text = "Apply Component";
        applyComponentButton.Click += ApplyComponentButton_Click;
        // 
        // blurKernelNumericUpDown
        // 
        blurKernelToolLabel.Text = "Blur";
        blurKernelNumericUpDown.Minimum = 1;
        blurKernelNumericUpDown.Maximum = 31;
        blurKernelNumericUpDown.Value = 3;
        blurKernelNumericUpDown.Width = 45;
        blurKernelNumericUpDown.ValueChanged += ComponentSettings_ValueChanged;
        // 
        // blackHatKernelNumericUpDown
        // 
        blackHatKernelToolLabel.Text = "BH";
        blackHatKernelNumericUpDown.Minimum = 3;
        blackHatKernelNumericUpDown.Maximum = 101;
        blackHatKernelNumericUpDown.Value = 11;
        blackHatKernelNumericUpDown.Width = 45;
        blackHatKernelNumericUpDown.ValueChanged += ComponentSettings_ValueChanged;
        // 
        // componentThresholdNumericUpDown
        // 
        componentThresholdToolLabel.Text = "Thr";
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
        componentMinAreaNumericUpDown.Minimum = 0;
        componentMinAreaNumericUpDown.Maximum = 1000000;
        componentMinAreaNumericUpDown.Value = 20;
        componentMinAreaNumericUpDown.Width = 65;
        componentMinAreaNumericUpDown.ValueChanged += ComponentSettings_ValueChanged;
        // 
        // componentMaxAreaNumericUpDown
        // 
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
        componentAspectToolLabel.Text = "H/W";
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
        // rectangleToolButton
        // 
        rectangleToolButton.Checked = true;
        rectangleToolButton.CheckOnClick = true;
        rectangleToolButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        rectangleToolButton.Text = "Rectangle";
        rectangleToolButton.Click += RectangleToolButton_Click;
        // 
        // brushToolButton
        // 
        brushToolButton.CheckOnClick = true;
        brushToolButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        brushToolButton.Text = "Brush";
        brushToolButton.Click += BrushToolButton_Click;
        // 
        // eraserToolButton
        // 
        eraserToolButton.CheckOnClick = true;
        eraserToolButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        eraserToolButton.Text = "Eraser";
        eraserToolButton.Click += EraserToolButton_Click;
        // 
        // measurementBoxToolButton
        // 
        measurementBoxToolButton.CheckOnClick = true;
        measurementBoxToolButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        measurementBoxToolButton.Text = "Box Tool";
        measurementBoxToolButton.Click += MeasurementBoxToolButton_Click;
        // 
        // brushSizeLabel
        // 
        brushSizeLabel.Text = "Brush";
        // 
        // brushSizeHost
        // 
        brushSizeHost.Name = "brushSizeHost";
        brushSizeHost.Size = new Size(60, 24);
        // 
        // brushSizeNumericUpDown
        // 
        var brushSizeNumericUpDown = (NumericUpDown)brushSizeHost.Control;
        brushSizeNumericUpDown.Minimum = 1;
        brushSizeNumericUpDown.Maximum = 200;
        brushSizeNumericUpDown.Value = 24;
        brushSizeNumericUpDown.Width = 60;
        brushSizeNumericUpDown.ValueChanged += BrushSizeNumericUpDown_ValueChanged;
        // 
        // fitToViewButton
        // 
        fitToViewButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        fitToViewButton.Text = "Fit";
        fitToViewButton.Click += FitToViewButton_Click;
        // 
        // mainSplitContainer
        // 
        mainSplitContainer.Dock = DockStyle.Fill;
        mainSplitContainer.FixedPanel = FixedPanel.Panel1;
        mainSplitContainer.Location = new Point(0, 52);
        mainSplitContainer.Name = "mainSplitContainer";
        // 
        // mainSplitContainer.Panel1
        // 
        mainSplitContainer.Panel1.Controls.Add(sideSplitContainer);
        // 
        // mainSplitContainer.Panel2
        // 
        mainSplitContainer.Panel2.Controls.Add(annotationCanvas);
        mainSplitContainer.Size = new Size(1100, 676);
        mainSplitContainer.SplitterDistance = 300;
        mainSplitContainer.TabIndex = 2;
        // 
        // sideSplitContainer
        // 
        sideSplitContainer.Dock = DockStyle.Fill;
        sideSplitContainer.Location = new Point(0, 0);
        sideSplitContainer.Name = "sideSplitContainer";
        sideSplitContainer.Orientation = Orientation.Horizontal;
        // 
        // sideSplitContainer.Panel1
        // 
        sideSplitContainer.Panel1.Controls.Add(imageListBox);
        // 
        // sideSplitContainer.Panel2
        // 
        sideSplitContainer.Panel2.Controls.Add(componentsGridView);
        sideSplitContainer.Size = new Size(300, 676);
        sideSplitContainer.SplitterDistance = 220;
        sideSplitContainer.TabIndex = 0;
        // 
        // imageListBox
        // 
        imageListBox.Dock = DockStyle.Fill;
        imageListBox.FormattingEnabled = true;
        imageListBox.IntegralHeight = false;
        imageListBox.Location = new Point(0, 0);
        imageListBox.Name = "imageListBox";
        imageListBox.Size = new Size(300, 220);
        imageListBox.TabIndex = 0;
        imageListBox.SelectedIndexChanged += ImageListBox_SelectedIndexChanged;
        // 
        // componentsGridView
        // 
        componentsGridView.AllowUserToAddRows = false;
        componentsGridView.AllowUserToDeleteRows = false;
        componentsGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        componentsGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        componentsGridView.Dock = DockStyle.Fill;
        componentsGridView.Location = new Point(0, 0);
        componentsGridView.Name = "componentsGridView";
        componentsGridView.ReadOnly = true;
        componentsGridView.RowHeadersVisible = false;
        componentsGridView.Size = new Size(300, 452);
        componentsGridView.TabIndex = 0;
        componentsGridView.SelectionChanged += ComponentsGridView_SelectionChanged;
        // 
        // annotationCanvas
        // 
        annotationCanvas.Dock = DockStyle.Fill;
        annotationCanvas.Location = new Point(0, 0);
        annotationCanvas.Name = "annotationCanvas";
        annotationCanvas.Size = new Size(796, 676);
        annotationCanvas.TabIndex = 0;
        annotationCanvas.TabStop = true;
        // 
        // statusStrip
        // 
        statusStrip.Items.AddRange(new ToolStripItem[] {
            centerXStatusLabel,
            centerYStatusLabel,
            angleStatusLabel,
            areaStatusLabel,
            componentStatusLabel});
        statusStrip.Location = new Point(0, 728);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(1100, 22);
        statusStrip.TabIndex = 2;
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
        // areaStatusLabel
        // 
        areaStatusLabel.Text = "Area: -";
        // 
        // componentStatusLabel
        // 
        componentStatusLabel.Text = "Components: 0";
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
        Text = "FeatureVision Annotation Tool";
        mainToolStrip.ResumeLayout(false);
        mainToolStrip.PerformLayout();
        componentToolStrip.ResumeLayout(false);
        componentToolStrip.PerformLayout();
        mainSplitContainer.Panel1.ResumeLayout(false);
        mainSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)mainSplitContainer).EndInit();
        mainSplitContainer.ResumeLayout(false);
        sideSplitContainer.Panel1.ResumeLayout(false);
        sideSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)sideSplitContainer).EndInit();
        sideSplitContainer.ResumeLayout(false);
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)brushSizeHost.Control).EndInit();
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
        ((System.ComponentModel.ISupportInitialize)componentsGridView).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}
