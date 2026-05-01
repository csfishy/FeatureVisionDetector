using System.Drawing;

namespace FeatureVisionDetector.WinForms.Models;

public sealed class FeatureResult
{
    public FeatureResult(Rectangle boundingBox, double area, double aspectRatio, double? shapeDistance = null)
    {
        BoundingBox = boundingBox;
        Area = area;
        AspectRatio = aspectRatio;
        ShapeDistance = shapeDistance;
    }

    public Rectangle BoundingBox { get; }

    public double Area { get; }

    public double AspectRatio { get; }

    public double? ShapeDistance { get; }
}
