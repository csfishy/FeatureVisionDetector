using System.Drawing;

namespace FeatureVisionDetector.WinForms.Models;

public sealed class DetectionSettings
{
    public bool IsDetectionEnabled { get; set; }

    public double ThresholdValue { get; set; } = 18D;

    public double MinArea { get; set; } = 20D;

    public double MaxArea { get; set; } = 2500D;

    public int MinWidth { get; set; } = 2;

    public int MaxWidth { get; set; } = 20;

    public int MinHeight { get; set; } = 45;

    public double MinAspectRatio { get; set; } = 4D;

    public double MaxShapeDistance { get; set; } = 0.35D;

    public double MinTemplateHeightRatio { get; set; } = 0.65D;

    public double MaxTemplateHeightRatio { get; set; } = 1.55D;

    public double MinTemplateWidthRatio { get; set; } = 0.2D;

    public double MaxTemplateWidthRatio { get; set; } = 1.8D;

    public double MinTemplateMatchScore { get; set; } = 0.35D;

    public Rectangle? Roi { get; set; }
}
