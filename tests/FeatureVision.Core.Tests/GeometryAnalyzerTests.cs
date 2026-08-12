using FeatureVision.Core.Geometry;
using OpenCvSharp;

namespace FeatureVision.Core.Tests;

public sealed class GeometryAnalyzerTests
{
    [Fact]
    public void AnalyzeMaskReturnsRectangleGeometry()
    {
        using var mask = new Mat(new Size(80, 60), MatType.CV_8UC1, Scalar.Black);
        Cv2.Rectangle(mask, new Rect(10, 20, 20, 10), Scalar.White, thickness: -1);

        var result = new GeometryAnalyzer().AnalyzeMask(mask);

        Assert.Equal(10, result.BoundingBox.X);
        Assert.Equal(20, result.BoundingBox.Y);
        Assert.Equal(20, result.BoundingBox.Width);
        Assert.Equal(10, result.BoundingBox.Height);
        Assert.Equal(200, result.Area);
        Assert.Equal(19.5, result.CenterX, precision: 6);
        Assert.Equal(24.5, result.CenterY, precision: 6);
        Assert.Equal(0, result.RotationAngleDegrees, precision: 6);
    }

    [Fact]
    public void AnalyzeMaskReturnsEmptyResultForEmptyMask()
    {
        using var mask = new Mat(new Size(20, 20), MatType.CV_8UC1, Scalar.Black);

        var result = new GeometryAnalyzer().AnalyzeMask(mask);

        Assert.Equal(0, result.Area);
        Assert.Equal(0, result.BoundingBox.Width);
        Assert.Equal(0, result.BoundingBox.Height);
    }
}
