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
        IReadOnlyList<Mat>? referenceMasks = null,
        bool applyScaleFilter = true)
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
        return FindComponentCandidates(
                binary,
                response,
                settings,
                referenceMasks ?? Array.Empty<Mat>(),
                applyScaleFilter)
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
        var candidate = FindComponentCandidates(
                binary,
                response,
                settings,
                Array.Empty<Mat>(),
                applyScaleFilter: false)
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
        IReadOnlyList<Mat> referenceMasks,
        bool applyScaleFilter)
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

        var referenceProfiles = CreateReferenceProfiles(referenceMasks, settings);
        var referenceDimensions = CreateReferenceDimensions(referenceMasks, settings);
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

            var aspectRatio = CalculateAspectRatio(rect, contour, settings);
            if (aspectRatio < settings.ComponentMinAspectRatio ||
                aspectRatio > settings.ComponentMaxAspectRatio)
            {
                continue;
            }

            var scale = CalculateScale(rect, contour, settings, referenceDimensions);
            if (applyScaleFilter &&
                referenceDimensions.Count > 0 &&
                (scale < settings.ScaleMin || scale > settings.ScaleMax))
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
            var localContour = OffsetContour(contour, -rect.X, -rect.Y);
            using var componentMask = new Mat(rect.Height, rect.Width, MatType.CV_8UC1, Scalar.Black);
            Cv2.DrawContours(componentMask, new[] { localContour }, 0, Scalar.White, -1);
            using var responseRoi = new Mat(response, rect);
            var responseScore = Cv2.Mean(responseRoi, componentMask).Val0 / 255.0;
            var candidateProfile = shapeScorer.CreateProfile(componentMask, settings);
            var shapeScore = CalculateShapeScore(candidateProfile, settings, referenceProfiles);
            var finalScore = CombineScores(shapeScore.Score, responseScore, settings, referenceProfiles.Count > 0);
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
                    ShapeScore = shapeScore.Score,
                    ShapeTransform = shapeScore.Transform,
                    ResponseScore = Math.Clamp(responseScore, 0.0, 1.0),
                    Scale = scale,
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

    private List<ComponentShapeProfile> CreateReferenceProfiles(
        IReadOnlyList<Mat> referenceMasks,
        DetectionSettings settings)
    {
        var profiles = new List<ComponentShapeProfile>(referenceMasks.Count);
        foreach (var referenceMask in referenceMasks)
        {
            if (referenceMask.Empty())
            {
                continue;
            }

            var profile = shapeScorer.CreateProfile(referenceMask, settings);
            if (profile.IsValid)
            {
                profiles.Add(profile);
            }
        }

        return profiles;
    }

    private static List<double> CreateReferenceDimensions(
        IReadOnlyList<Mat> referenceMasks,
        DetectionSettings settings)
    {
        var dimensions = new List<double>(referenceMasks.Count);
        foreach (var referenceMask in referenceMasks)
        {
            var dimension = FindForegroundDimension(referenceMask, settings);
            if (dimension > 0.0)
            {
                dimensions.Add(dimension);
            }
        }

        return dimensions;
    }

    private ComponentShapeScoreResult CalculateShapeScore(
        ComponentShapeProfile candidateProfile,
        DetectionSettings settings,
        IReadOnlyList<ComponentShapeProfile> referenceProfiles)
    {
        var bestResult = new ComponentShapeScoreResult(0.0, string.Empty);
        if (!candidateProfile.IsValid)
        {
            return bestResult;
        }

        foreach (var referenceProfile in referenceProfiles)
        {
            var result = shapeScorer.ScoreDetailed(referenceProfile, candidateProfile, settings);
            if (result.Score > bestResult.Score)
            {
                bestResult = result;
            }
        }

        return bestResult;
    }

    private static double CalculateAspectRatio(
        Rect rect,
        IReadOnlyList<Point> componentPoints,
        DetectionSettings settings)
    {
        if (!settings.ComponentShapeNormalizeRotation)
        {
            return rect.Height / (double)Math.Max(1, rect.Width);
        }

        var bounds = CalculateOrientedBounds(componentPoints);
        if (!bounds.IsValid)
        {
            var longSide = Math.Max(rect.Width, rect.Height);
            var shortSide = Math.Max(1, Math.Min(rect.Width, rect.Height));
            return longSide / (double)shortSide;
        }

        return bounds.MajorLength / Math.Max(1.0, bounds.MinorLength);
    }

    private static double CalculateScale(
        Rect componentRect,
        IReadOnlyList<Point> componentPoints,
        DetectionSettings settings,
        IReadOnlyList<double> referenceDimensions)
    {
        if (referenceDimensions.Count == 0)
        {
            return 1.0;
        }

        var componentLength = CalculateReferenceDimension(componentRect, componentPoints, settings);
        var bestScale = 1.0;
        var bestDistance = double.MaxValue;
        foreach (var referenceLength in referenceDimensions)
        {
            if (referenceLength <= 0)
            {
                continue;
            }

            var scale = componentLength / (double)referenceLength;
            var distance = Math.Abs(scale - 1.0);
            if (distance >= bestDistance)
            {
                continue;
            }

            bestScale = scale;
            bestDistance = distance;
        }

        return bestScale;
    }

    private static Point[] OffsetContour(IReadOnlyList<Point> contour, int offsetX, int offsetY)
    {
        var offsetContour = new Point[contour.Count];
        for (var index = 0; index < contour.Count; index++)
        {
            var point = contour[index];
            offsetContour[index] = new Point(point.X + offsetX, point.Y + offsetY);
        }

        return offsetContour;
    }

    private static double FindForegroundDimension(Mat mask, DetectionSettings settings)
    {
        if (mask.Empty())
        {
            return 0;
        }

        using var binaryMask = CreateBinaryMask(mask);
        Cv2.FindContours(
            binaryMask,
            out Point[][] contours,
            out _,
            RetrievalModes.External,
            ContourApproximationModes.ApproxSimple);

        if (contours.Length == 0)
        {
            return 0;
        }

        var rect = Cv2.BoundingRect(contours[0]);
        for (var index = 1; index < contours.Length; index++)
        {
            rect = Union(rect, Cv2.BoundingRect(contours[index]));
        }

        var points = CollectForegroundPoints(binaryMask);
        return CalculateReferenceDimension(rect, points, settings);
    }

    private static double CalculateReferenceDimension(
        Rect rect,
        IReadOnlyList<Point> points,
        DetectionSettings settings)
    {
        if (!settings.ComponentShapeNormalizeRotation)
        {
            return rect.Height;
        }

        var bounds = CalculateOrientedBounds(points);
        return bounds.IsValid
            ? bounds.MajorLength
            : Math.Max(rect.Width, rect.Height);
    }

    private static List<Point> CollectForegroundPoints(Mat binary)
    {
        var points = new List<Point>();
        for (var y = 0; y < binary.Rows; y++)
        {
            for (var x = 0; x < binary.Cols; x++)
            {
                if (binary.At<byte>(y, x) != 0)
                {
                    points.Add(new Point(x, y));
                }
            }
        }

        return points;
    }

    private static OrientedBounds CalculateOrientedBounds(IReadOnlyList<Point> points)
    {
        if (points.Count < 2)
        {
            return OrientedBounds.Invalid;
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

        if (Math.Abs(covarianceXx) < double.Epsilon &&
            Math.Abs(covarianceXy) < double.Epsilon &&
            Math.Abs(covarianceYy) < double.Epsilon)
        {
            return OrientedBounds.Invalid;
        }

        var angle = Math.Atan2(2.0 * covarianceXy, covarianceXx - covarianceYy) * 0.5;
        var axisX = Math.Cos(angle);
        var axisY = Math.Sin(angle);
        var lateralX = -axisY;
        var lateralY = axisX;
        var minMajor = double.MaxValue;
        var maxMajor = double.MinValue;
        var minMinor = double.MaxValue;
        var maxMinor = double.MinValue;

        foreach (var point in points)
        {
            var dx = point.X - meanX;
            var dy = point.Y - meanY;
            var major = dx * axisX + dy * axisY;
            var minor = dx * lateralX + dy * lateralY;
            minMajor = Math.Min(minMajor, major);
            maxMajor = Math.Max(maxMajor, major);
            minMinor = Math.Min(minMinor, minor);
            maxMinor = Math.Max(maxMinor, minor);
        }

        var majorLength = maxMajor - minMajor;
        var minorLength = maxMinor - minMinor;
        if (majorLength <= 0.0)
        {
            return OrientedBounds.Invalid;
        }

        return new OrientedBounds(
            majorLength,
            Math.Max(1.0, minorLength),
            IsValid: true);
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

    private static Rect Union(Rect first, Rect second)
    {
        var left = Math.Min(first.Left, second.Left);
        var top = Math.Min(first.Top, second.Top);
        var right = Math.Max(first.Right, second.Right);
        var bottom = Math.Max(first.Bottom, second.Bottom);
        return Rect.FromLTRB(left, top, right, bottom);
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

    private readonly record struct OrientedBounds(
        double MajorLength,
        double MinorLength,
        bool IsValid)
    {
        public static OrientedBounds Invalid { get; } = new(0, 0, IsValid: false);
    }
}
