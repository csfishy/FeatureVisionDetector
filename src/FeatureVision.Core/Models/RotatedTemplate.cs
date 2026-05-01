using OpenCvSharp;

namespace FeatureVision.Core.Models;

public sealed class RotatedTemplate : IDisposable
{
    public Mat Image { get; init; } = new();

    public Mat Mask { get; init; } = new();

    public double RotationAngleDegrees { get; init; }

    public double Scale { get; init; } = 1.0;

    public double CenterOffsetX { get; init; }

    public double CenterOffsetY { get; init; }

    public void Dispose()
    {
        Image.Dispose();
        Mask.Dispose();
    }
}
