using FeatureVision.Core.Models;
using OpenCvSharp;

namespace FeatureVision.Core.Matching;

public sealed class RotatedTemplateGenerator
{
    public IReadOnlyList<RotatedTemplate> Generate(
        Mat sampleImage,
        Mat mask,
        DetectionSettings settings)
    {
        return Generate(
            sampleImage,
            mask,
            settings,
            new Point2d(sampleImage.Width / 2.0, sampleImage.Height / 2.0));
    }

    public IReadOnlyList<RotatedTemplate> Generate(
        Mat sampleImage,
        Mat mask,
        DetectionSettings settings,
        Point2d centerOffset)
    {
        ArgumentNullException.ThrowIfNull(sampleImage);
        ArgumentNullException.ThrowIfNull(mask);
        ArgumentNullException.ThrowIfNull(settings);

        if (sampleImage.Empty() || mask.Empty())
        {
            return Array.Empty<RotatedTemplate>();
        }

        if (sampleImage.Width != mask.Width || sampleImage.Height != mask.Height)
        {
            throw new ArgumentException("Sample image and mask dimensions must match.");
        }

        using var binaryMask = CreateBinaryMask(mask);
        var templates = new List<RotatedTemplate>();
        foreach (var angle in EnumerateAngles(settings.AngleMin, settings.AngleMax, settings.AngleStep))
        {
            var rotatedTemplate = RotateTemplate(sampleImage, binaryMask, angle, centerOffset);
            if (Cv2.CountNonZero(rotatedTemplate.Mask) == 0)
            {
                rotatedTemplate.Dispose();
                continue;
            }

            templates.Add(rotatedTemplate);
        }

        return templates;
    }

    private static RotatedTemplate RotateTemplate(
        Mat image,
        Mat mask,
        double angleDegrees,
        Point2d centerOffset)
    {
        var imageWidth = image.Width;
        var imageHeight = image.Height;
        var imageCenter = new Point2f(imageWidth / 2.0f, imageHeight / 2.0f);

        using var rotationMatrix = Cv2.GetRotationMatrix2D(imageCenter, angleDegrees, 1.0);
        var cos = Math.Abs(rotationMatrix.At<double>(0, 0));
        var sin = Math.Abs(rotationMatrix.At<double>(0, 1));
        var rotatedWidth = Math.Max(1, (int)Math.Ceiling(imageHeight * sin + imageWidth * cos));
        var rotatedHeight = Math.Max(1, (int)Math.Ceiling(imageHeight * cos + imageWidth * sin));

        rotationMatrix.Set(0, 2, rotationMatrix.At<double>(0, 2) + rotatedWidth / 2.0 - imageCenter.X);
        rotationMatrix.Set(1, 2, rotationMatrix.At<double>(1, 2) + rotatedHeight / 2.0 - imageCenter.Y);

        var rotatedImage = new Mat();
        var rotatedMask = new Mat();
        var outputSize = new Size(rotatedWidth, rotatedHeight);

        Cv2.WarpAffine(
            image,
            rotatedImage,
            rotationMatrix,
            outputSize,
            InterpolationFlags.Linear,
            BorderTypes.Constant,
            Scalar.Black);

        Cv2.WarpAffine(
            mask,
            rotatedMask,
            rotationMatrix,
            outputSize,
            InterpolationFlags.Nearest,
            BorderTypes.Constant,
            Scalar.Black);

        Cv2.Threshold(rotatedMask, rotatedMask, 0, 255, ThresholdTypes.Binary);

        var centerX = rotationMatrix.At<double>(0, 0) * centerOffset.X
            + rotationMatrix.At<double>(0, 1) * centerOffset.Y
            + rotationMatrix.At<double>(0, 2);
        var centerY = rotationMatrix.At<double>(1, 0) * centerOffset.X
            + rotationMatrix.At<double>(1, 1) * centerOffset.Y
            + rotationMatrix.At<double>(1, 2);

        return new RotatedTemplate
        {
            Image = rotatedImage,
            Mask = rotatedMask,
            RotationAngleDegrees = angleDegrees,
            CenterOffsetX = centerX,
            CenterOffsetY = centerY
        };
    }

    private static Mat CreateBinaryMask(Mat mask)
    {
        Mat grayscale;
        if (mask.Channels() == 1)
        {
            grayscale = mask.Clone();
        }
        else
        {
            grayscale = new Mat();
            var conversion = mask.Channels() == 4
                ? ColorConversionCodes.BGRA2GRAY
                : ColorConversionCodes.BGR2GRAY;
            Cv2.CvtColor(mask, grayscale, conversion);
        }

        var binary = new Mat();
        Cv2.Threshold(grayscale, binary, 0, 255, ThresholdTypes.Binary);
        grayscale.Dispose();
        return binary;
    }

    private static IEnumerable<double> EnumerateAngles(double angleMin, double angleMax, double angleStep)
    {
        if (angleStep <= 0)
        {
            angleStep = 1.0;
        }

        if (angleMin > angleMax)
        {
            (angleMin, angleMax) = (angleMax, angleMin);
        }

        var count = 0;
        for (var angle = angleMin; angle <= angleMax + angleStep * 0.5; angle += angleStep)
        {
            yield return Math.Min(angle, angleMax);

            count++;
            if (count >= 721)
            {
                yield break;
            }
        }
    }
}
