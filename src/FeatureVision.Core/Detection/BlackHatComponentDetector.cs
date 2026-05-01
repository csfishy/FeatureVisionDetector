using FeatureVision.Core.Matching;
using FeatureVision.Core.Models;
using OpenCvSharp;

namespace FeatureVision.Core.Detection;

public sealed class BlackHatComponentDetector
{
    private readonly ComponentShapeScorer shapeScorer = new();

    public IReadOnlyList<ConnectedComponentResult> Detect(
        Mat frame,
        DetectionSettings settings,
        IReadOnlyList<Mat>? referenceMasks = null)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(settings);

        if (frame.Empty())
        {
            return Array.Empty<ConnectedComponentResult>();
        }

        using var gray = TemplateFeatureMatcher.ToGray(frame);
        using var response = TemplateFeatureMatcher.CreateDarkFeatureResponse(gray, settings);
        using var binary = CreateBinaryMask(response, settings);
        return FindComponentCandidates(binary, response, settings, referenceMasks ?? Array.Empty<Mat>())
            .Select(candidate => candidate.Result)
            .ToList();
    }

    public Mat CreateComponentMask(Mat frame, DetectionSettings settings, int componentId)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(settings);

        if (frame.Empty() || componentId <= 0)
        {
            return new Mat();
        }

        using var gray = TemplateFeatureMatcher.ToGray(frame);
        using var response = TemplateFeatureMatcher.CreateDarkFeatureResponse(gray, settings);
        using var binary = CreateBinaryMask(response, settings);
        var candidate = FindComponentCandidates(binary, response, settings, Array.Empty<Mat>())
            .FirstOrDefault(component => component.Result.Id == componentId);

        if (candidate is null)
        {
            return new Mat(binary.Size(), MatType.CV_8UC1, Scalar.Black);
        }

        var mask = new Mat(binary.Size(), MatType.CV_8UC1, Scalar.Black);
        Cv2.DrawContours(mask, new[] { candidate.Contour }, 0, Scalar.White, -1);
        return mask;
    }

    public Mat CreateBinaryMask(Mat blackHatResponse, DetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(blackHatResponse);
        ArgumentNullException.ThrowIfNull(settings);

        if (blackHatResponse.Empty())
        {
            return new Mat();
        }

        var threshold = Math.Clamp(settings.ComponentThreshold, 0.0, 255.0);
        var binary = new Mat();
        Cv2.Threshold(blackHatResponse, binary, threshold, 255, ThresholdTypes.Binary);

        ApplyMorphology(binary, MorphTypes.Open, settings.ComponentOpenKernelSize);
        ApplyMorphology(binary, MorphTypes.Close, settings.ComponentCloseKernelSize);
        return binary;
    }

    private IReadOnlyList<ComponentCandidate> FindComponentCandidates(
        Mat binary,
        Mat response,
        DetectionSettings settings,
        IReadOnlyList<Mat> referenceMasks)
    {
        if (binary.Empty())
        {
            return Array.Empty<ComponentCandidate>();
        }

        Cv2.FindContours(
            binary,
            out Point[][] contours,
            out _,
            RetrievalModes.External,
            ContourApproximationModes.ApproxSimple);

        var components = new List<ComponentCandidate>();
        foreach (var contour in contours)
        {
            var area = Cv2.ContourArea(contour);
            if (area < settings.ComponentMinArea || area > settings.ComponentMaxArea)
            {
                continue;
            }

            var rect = Cv2.BoundingRect(contour);
            if (rect.Width < settings.ComponentMinWidth ||
                rect.Width > settings.ComponentMaxWidth ||
                rect.Height < settings.ComponentMinHeight ||
                rect.Height > settings.ComponentMaxHeight)
            {
                continue;
            }

            var aspectRatio = rect.Height / (double)Math.Max(1, rect.Width);
            if (aspectRatio < settings.ComponentMinAspectRatio ||
                aspectRatio > settings.ComponentMaxAspectRatio)
            {
                continue;
            }

            var moments = Cv2.Moments(contour);
            var centerX = Math.Abs(moments.M00) > double.Epsilon
                ? moments.M10 / moments.M00
                : rect.X + rect.Width / 2.0;
            var centerY = Math.Abs(moments.M00) > double.Epsilon
                ? moments.M01 / moments.M00
                : rect.Y + rect.Height / 2.0;
            using var componentMask = new Mat(binary.Size(), MatType.CV_8UC1, Scalar.Black);
            Cv2.DrawContours(componentMask, new[] { contour }, 0, Scalar.White, -1);
            var responseScore = Cv2.Mean(response, componentMask).Val0 / 255.0;
            var shapeScore = CalculateShapeScore(componentMask, settings, referenceMasks);
            var finalScore = CombineScores(shapeScore, responseScore, settings, referenceMasks.Count > 0);
            var angle = CalculatePcaAngle(contour);

            components.Add(new ComponentCandidate
            {
                Contour = contour,
                Result = new ConnectedComponentResult
                {
                    Center = new Point2D
                    {
                        X = centerX,
                        Y = centerY
                    },
                    BoundingBox = new RoiRect
                    {
                        X = rect.X,
                        Y = rect.Y,
                        Width = rect.Width,
                        Height = rect.Height
                    },
                    AreaPixels = area,
                    RotationAngleDegrees = NormalizeAngle(angle),
                    Score = finalScore,
                    ShapeScore = shapeScore,
                    ResponseScore = Math.Clamp(responseScore, 0.0, 1.0),
                    AspectRatio = aspectRatio
                }
            });
        }

        return components
            .OrderByDescending(component => component.Result.AreaPixels)
            .Select((component, index) =>
            {
                component.Result.Id = index + 1;
                return component;
            })
            .ToList();
    }

    private static void ApplyMorphology(Mat binary, MorphTypes operation, int kernelSize)
    {
        kernelSize = NormalizeOddKernelSize(kernelSize, minimum: 1);
        if (kernelSize <= 1)
        {
            return;
        }

        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(kernelSize, kernelSize));
        Cv2.MorphologyEx(binary, binary, operation, kernel);
    }

    private static int NormalizeOddKernelSize(int value, int minimum)
    {
        var normalized = Math.Max(minimum, value);
        return normalized % 2 == 0 ? normalized + 1 : normalized;
    }

    private double CalculateShapeScore(
        Mat componentMask,
        DetectionSettings settings,
        IReadOnlyList<Mat> referenceMasks)
    {
        var bestScore = 0.0;
        foreach (var referenceMask in referenceMasks)
        {
            if (referenceMask.Empty())
            {
                continue;
            }

            bestScore = Math.Max(bestScore, shapeScorer.Score(referenceMask, componentMask, settings));
        }

        return bestScore;
    }

    private static double CombineScores(
        double shapeScore,
        double responseScore,
        DetectionSettings settings,
        bool hasReferenceShape)
    {
        responseScore = Math.Clamp(responseScore, 0.0, 1.0);
        if (!hasReferenceShape)
        {
            return responseScore;
        }

        var shapeWeight = Math.Clamp(settings.ComponentShapeScoreWeight, 0.0, 1.0);
        return Math.Clamp(shapeScore * shapeWeight + responseScore * (1.0 - shapeWeight), 0.0, 1.0);
    }

    private static double CalculatePcaAngle(IReadOnlyList<Point> points)
    {
        if (points.Count < 2)
        {
            return 0.0;
        }

        var meanX = 0.0;
        var meanY = 0.0;
        foreach (var point in points)
        {
            meanX += point.X;
            meanY += point.Y;
        }

        meanX /= points.Count;
        meanY /= points.Count;

        var covarianceXx = 0.0;
        var covarianceXy = 0.0;
        var covarianceYy = 0.0;
        foreach (var point in points)
        {
            var dx = point.X - meanX;
            var dy = point.Y - meanY;
            covarianceXx += dx * dx;
            covarianceXy += dx * dy;
            covarianceYy += dy * dy;
        }

        return Math.Atan2(2.0 * covarianceXy, covarianceXx - covarianceYy) * 0.5 * 180.0 / Math.PI;
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

    private sealed class ComponentCandidate
    {
        public ConnectedComponentResult Result { get; init; } = new();

        public Point[] Contour { get; init; } = Array.Empty<Point>();
    }
}
