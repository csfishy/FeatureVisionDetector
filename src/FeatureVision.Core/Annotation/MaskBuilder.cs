using OpenCvSharp;

namespace FeatureVision.Core.Annotation;

public sealed class MaskBuilder
{
    public Mat BuildFromRectangle(Size imageSize, Rect rectangle)
    {
        throw new NotImplementedException();
    }

    public Mat BuildFromPolygon(Size imageSize, IReadOnlyList<Point> polygon)
    {
        throw new NotImplementedException();
    }

    public void ApplyBrushStroke(Mat mask, Point center, int radius)
    {
        throw new NotImplementedException();
    }

    public void ApplyEraserStroke(Mat mask, Point center, int radius)
    {
        throw new NotImplementedException();
    }
}
