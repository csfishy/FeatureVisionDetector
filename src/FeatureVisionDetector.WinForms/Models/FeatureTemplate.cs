using OpenCvSharp;
using CvPoint = OpenCvSharp.Point;
using Rectangle = System.Drawing.Rectangle;

namespace FeatureVisionDetector.WinForms.Models;

public sealed class FeatureTemplate : IDisposable
{
    public FeatureTemplate(
        CvPoint[] contour,
        Rectangle boundingBox,
        double area,
        double aspectRatio,
        Mat processedImage,
        IReadOnlyList<double> centerlineProfile)
    {
        ArgumentNullException.ThrowIfNull(contour);
        ArgumentNullException.ThrowIfNull(processedImage);
        ArgumentNullException.ThrowIfNull(centerlineProfile);

        Contour = contour;
        BoundingBox = boundingBox;
        Area = area;
        AspectRatio = aspectRatio;
        ProcessedImage = processedImage.Clone();
        CenterlineProfile = centerlineProfile.ToArray();
    }

    public CvPoint[] Contour { get; }

    public Rectangle BoundingBox { get; }

    public double Area { get; }

    public double AspectRatio { get; }

    public Mat ProcessedImage { get; }

    public IReadOnlyList<double> CenterlineProfile { get; }

    public void Dispose()
    {
        ProcessedImage.Dispose();
    }
}
