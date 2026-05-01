using FeatureVision.Core.Models;
using OpenCvSharp;

namespace FeatureVision.Core.Matching;

public sealed class TemplateFeatureMatcher
{
    private readonly NonMaximumSuppression nonMaximumSuppression = new();
    private readonly RotatedTemplateGenerator rotatedTemplateGenerator = new();

    public IReadOnlyList<DetectionResult> Match(
        Mat frame,
        FeatureModel featureModel,
        DetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(featureModel);
        ArgumentNullException.ThrowIfNull(settings);

        if (frame.Empty() || featureModel.Samples.Count == 0)
        {
            return Array.Empty<DetectionResult>();
        }

        using var frameGray = ToGray(frame);
        using var frameResponse = CreateDarkFeatureResponse(frameGray, settings);
        var candidates = new List<DetectionResult>();

        foreach (var sample in featureModel.Samples)
        {
            candidates.AddRange(MatchSample(frameResponse, sample, settings));
        }

        return nonMaximumSuppression
            .Suppress(candidates, settings.NmsOverlapThreshold)
            .Take(settings.MaximumDetections)
            .ToList();
    }

    private IReadOnlyList<DetectionResult> MatchSample(
        Mat frameResponse,
        FeatureSample sample,
        DetectionSettings settings)
    {
        if (!File.Exists(sample.ImagePath))
        {
            throw new FileNotFoundException("The sample image file could not be found.", sample.ImagePath);
        }

        if (!File.Exists(sample.MaskPath))
        {
            throw new FileNotFoundException("The sample mask file could not be found.", sample.MaskPath);
        }

        using var sampleImage = Cv2.ImRead(sample.ImagePath, ImreadModes.Color);
        using var sampleMask = Cv2.ImRead(sample.MaskPath, ImreadModes.Grayscale);
        if (sampleImage.Empty() || sampleMask.Empty())
        {
            return Array.Empty<DetectionResult>();
        }

        using var templateGrayFull = ToGray(sampleImage);
        using var templateResponseFull = CreateDarkFeatureResponse(templateGrayFull, settings);
        using var maskBinaryFull = new Mat();
        Cv2.Threshold(sampleMask, maskBinaryFull, 0, 255, ThresholdTypes.Binary);

        var templateRect = GetTemplateRect(sample, templateResponseFull.Size());
        if (templateRect.Width <= 0 || templateRect.Height <= 0)
        {
            return Array.Empty<DetectionResult>();
        }

        using var templateResponse = new Mat(templateResponseFull, templateRect).Clone();
        using var templateMask = new Mat(maskBinaryFull, templateRect).Clone();
        using var refinedTemplateMask = RefineTemplateMask(templateResponse, templateMask, settings);
        if (Cv2.CountNonZero(refinedTemplateMask) == 0)
        {
            return Array.Empty<DetectionResult>();
        }

        var centerOffsetX = sample.Center.X - templateRect.X;
        var centerOffsetY = sample.Center.Y - templateRect.Y;
        var rotatedTemplates = rotatedTemplateGenerator.Generate(
            templateResponse,
            refinedTemplateMask,
            settings,
            new Point2d(centerOffsetX, centerOffsetY));

        try
        {
            var candidates = new List<DetectionResult>();
            foreach (var rotatedTemplate in rotatedTemplates)
            {
                candidates.AddRange(MatchRotatedTemplate(frameResponse, sample, rotatedTemplate, settings.ScoreThreshold));
            }

            return candidates;
        }
        finally
        {
            foreach (var rotatedTemplate in rotatedTemplates)
            {
                rotatedTemplate.Dispose();
            }
        }
    }

    private static IReadOnlyList<DetectionResult> MatchRotatedTemplate(
        Mat frameResponse,
        FeatureSample sample,
        RotatedTemplate rotatedTemplate,
        double scoreThreshold)
    {
        if (rotatedTemplate.Image.Width > frameResponse.Width ||
            rotatedTemplate.Image.Height > frameResponse.Height ||
            rotatedTemplate.Image.Width <= 0 ||
            rotatedTemplate.Image.Height <= 0)
        {
            return Array.Empty<DetectionResult>();
        }

        using var result = new Mat();
        Cv2.MatchTemplate(
            frameResponse,
            rotatedTemplate.Image,
            result,
            TemplateMatchModes.CCorrNormed,
            rotatedTemplate.Mask);

        return CollectTemplateMatches(result, sample, rotatedTemplate, scoreThreshold);
    }

    private static IReadOnlyList<DetectionResult> CollectTemplateMatches(
        Mat result,
        FeatureSample sample,
        RotatedTemplate rotatedTemplate,
        double scoreThreshold)
    {
        var matches = new List<DetectionResult>();

        for (var y = 0; y < result.Rows; y++)
        {
            for (var x = 0; x < result.Cols; x++)
            {
                var score = result.At<float>(y, x);
                if (float.IsNaN(score) ||
                    float.IsInfinity(score) ||
                    score < scoreThreshold ||
                    !IsLocalMaximum(result, x, y, score))
                {
                    continue;
                }

                matches.Add(new DetectionResult
                {
                    FeatureSampleId = sample.Id,
                    Center = new Point2D
                    {
                        X = x + rotatedTemplate.CenterOffsetX,
                        Y = y + rotatedTemplate.CenterOffsetY
                    },
                    RotationAngleDegrees = NormalizeAngle(sample.RotationAngleDegrees + rotatedTemplate.RotationAngleDegrees),
                    MatchingScore = score,
                    BoundingBox = new RoiRect
                    {
                        X = x,
                        Y = y,
                        Width = rotatedTemplate.Image.Width,
                        Height = rotatedTemplate.Image.Height
                    }
                });
            }
        }

        return matches;
    }

    private static bool IsLocalMaximum(Mat result, int x, int y, float score)
    {
        var startX = Math.Max(0, x - 1);
        var endX = Math.Min(result.Cols - 1, x + 1);
        var startY = Math.Max(0, y - 1);
        var endY = Math.Min(result.Rows - 1, y + 1);

        for (var neighborY = startY; neighborY <= endY; neighborY++)
        {
            for (var neighborX = startX; neighborX <= endX; neighborX++)
            {
                if (neighborX == x && neighborY == y)
                {
                    continue;
                }

                var neighborScore = result.At<float>(neighborY, neighborX);
                if (!float.IsNaN(neighborScore) &&
                    !float.IsInfinity(neighborScore) &&
                    neighborScore > score)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static Mat ToGray(Mat image)
    {
        if (image.Channels() == 1)
        {
            return image.Clone();
        }

        var gray = new Mat();
        var conversion = image.Channels() == 4
            ? ColorConversionCodes.BGRA2GRAY
            : ColorConversionCodes.BGR2GRAY;

        Cv2.CvtColor(image, gray, conversion);
        return gray;
    }

    public static Mat CreateBlurredGrayscale(Mat grayscale, DetectionSettings settings)
    {
        var kernelSize = NormalizeOddKernelSize(settings.BlurKernelSize, minimum: 1);
        if (kernelSize <= 1)
        {
            return grayscale.Clone();
        }

        using var blurred = new Mat();
        Cv2.GaussianBlur(grayscale, blurred, new Size(kernelSize, kernelSize), 0);
        return blurred.Clone();
    }

    public static Mat CreateDarkFeatureResponse(Mat grayscale, DetectionSettings settings)
    {
        using var blurred = CreateBlurredGrayscale(grayscale, settings);
        var blackHatKernelSize = NormalizeOddKernelSize(settings.BlackHatKernelSize, minimum: 3);
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(blackHatKernelSize, blackHatKernelSize));
        var response = new Mat();
        Cv2.MorphologyEx(blurred, response, MorphTypes.BlackHat, kernel);
        Cv2.Normalize(response, response, 0, 255, NormTypes.MinMax);
        return response;
    }

    private static Mat RefineTemplateMask(Mat templateResponse, Mat annotationMask, DetectionSettings settings)
    {
        using var responseInsideAnnotation = new Mat();
        Cv2.BitwiseAnd(templateResponse, templateResponse, responseInsideAnnotation, annotationMask);

        var refinedMask = new Mat();
        Cv2.Threshold(responseInsideAnnotation, refinedMask, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
        Cv2.BitwiseAnd(refinedMask, annotationMask, refinedMask);

        var annotationPixels = Cv2.CountNonZero(annotationMask);
        var refinedPixels = Cv2.CountNonZero(refinedMask);
        if (annotationPixels == 0)
        {
            refinedMask.Dispose();
            return annotationMask.Clone();
        }

        if (refinedPixels < Math.Max(8, annotationPixels * 0.01))
        {
            refinedMask.Dispose();
            return annotationMask.Clone();
        }

        var dilateKernelSize = NormalizeOddKernelSize(settings.MaskRefinementDilateSize, minimum: 1);
        using var dilateKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(dilateKernelSize, dilateKernelSize));
        Cv2.Dilate(refinedMask, refinedMask, dilateKernel);
        Cv2.BitwiseAnd(refinedMask, annotationMask, refinedMask);
        return refinedMask;
    }

    private static int NormalizeOddKernelSize(int value, int minimum)
    {
        var normalized = Math.Max(minimum, value);
        if (normalized % 2 == 0)
        {
            normalized++;
        }

        return normalized;
    }

    private static Rect GetTemplateRect(FeatureSample sample, Size imageSize)
    {
        var x = sample.BoundingBox.X;
        var y = sample.BoundingBox.Y;
        var width = sample.BoundingBox.Width;
        var height = sample.BoundingBox.Height;

        if (width <= 0 || height <= 0)
        {
            return new Rect(0, 0, imageSize.Width, imageSize.Height);
        }

        x = Math.Clamp(x, 0, imageSize.Width - 1);
        y = Math.Clamp(y, 0, imageSize.Height - 1);
        width = Math.Min(width, imageSize.Width - x);
        height = Math.Min(height, imageSize.Height - y);

        return new Rect(x, y, width, height);
    }

    private static double NormalizeAngle(double angleDegrees)
    {
        while (angleDegrees <= -180.0)
        {
            angleDegrees += 360.0;
        }

        while (angleDegrees > 180.0)
        {
            angleDegrees -= 360.0;
        }

        return angleDegrees;
    }
}
