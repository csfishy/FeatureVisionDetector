using FeatureVision.Core.Models;
using OpenCvSharp;

namespace FeatureVision.Core.Detection;

public sealed class ComponentShapeScorer
{
    public double Score(Mat referenceMask, Mat candidateMask, DetectionSettings settings)
    {
        return ScoreDetailed(referenceMask, candidateMask, settings).Score;
    }

    public ComponentShapeScoreResult ScoreDetailed(Mat referenceMask, Mat candidateMask, DetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(referenceMask);
        ArgumentNullException.ThrowIfNull(candidateMask);
        ArgumentNullException.ThrowIfNull(settings);

        if (referenceMask.Empty() || candidateMask.Empty())
        {
            return new ComponentShapeScoreResult(0.0, string.Empty);
        }

        var referenceProfile = CreateProfile(referenceMask, settings);
        var candidateProfile = CreateProfile(candidateMask, settings);
        return ScoreDetailed(referenceProfile, candidateProfile, settings);
    }

    public ComponentShapeProfile CreateProfile(Mat mask, DetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(mask);
        ArgumentNullException.ThrowIfNull(settings);

        if (mask.Empty())
        {
            return ComponentShapeProfile.Empty;
        }

        var sampleCount = Math.Clamp(settings.ComponentShapeProfileSamples, 16, 256);
        var values = CreateProfileValues(
            mask,
            sampleCount,
            settings.ComponentShapeNormalizeRotation);
        return values.Length == 0
            ? ComponentShapeProfile.Empty
            : new ComponentShapeProfile(values);
    }

    public ComponentShapeScoreResult ScoreDetailed(
        ComponentShapeProfile referenceProfile,
        ComponentShapeProfile candidateProfile,
        DetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!referenceProfile.IsValid || !candidateProfile.IsValid)
        {
            return new ComponentShapeScoreResult(0.0, string.Empty);
        }

        var bestDistance = double.MaxValue;
        var bestTransform = "normal";
        CompareCandidateProfile(referenceProfile.Values, candidateProfile.Values, "normal", ref bestDistance, ref bestTransform);
        CompareCandidateProfile(referenceProfile.Values, ReverseProfile(candidateProfile.Values), "axis-reversed", ref bestDistance, ref bestTransform);
        if (settings.ComponentShapeAllowFlip)
        {
            var mirroredProfile = NegateProfile(candidateProfile.Values);
            CompareCandidateProfile(referenceProfile.Values, mirroredProfile, "mirrored", ref bestDistance, ref bestTransform);
            CompareCandidateProfile(
                referenceProfile.Values,
                ReverseProfile(mirroredProfile),
                "mirrored-axis-reversed",
                ref bestDistance,
                ref bestTransform);
        }

        var sensitivity = Math.Max(0.1, settings.ComponentShapeDistanceSensitivity);
        var score = Math.Clamp(Math.Exp(-bestDistance * sensitivity), 0.0, 1.0);
        return new ComponentShapeScoreResult(score, bestTransform);
    }

    private static double[] CreateProfileValues(Mat mask, int sampleCount, bool normalizeRotation)
    {
        return normalizeRotation
            ? CreatePrincipalAxisProfile(mask, sampleCount)
            : CreateVerticalProfile(mask, sampleCount);
    }

    private static double[] CreatePrincipalAxisProfile(Mat mask, int sampleCount)
    {
        using var binary = CreateBinaryMask(mask);
        var points = CollectForegroundPoints(binary);
        if (points.Count < 2)
        {
            return Array.Empty<double>();
        }

        var axes = CalculatePrincipalAxes(points);
        if (!axes.IsValid)
        {
            return Array.Empty<double>();
        }

        var minT = double.MaxValue;
        var maxT = double.MinValue;
        foreach (var point in points)
        {
            var dx = point.X - axes.CenterX;
            var dy = point.Y - axes.CenterY;
            var t = dx * axes.AxisX + dy * axes.AxisY;
            minT = Math.Min(minT, t);
            maxT = Math.Max(maxT, t);
        }

        var majorLength = maxT - minT;
        if (majorLength <= 1.0)
        {
            return Array.Empty<double>();
        }

        var profile = new double[sampleCount];
        var counts = new int[sampleCount];
        Array.Fill(profile, double.NaN);

        foreach (var point in points)
        {
            var dx = point.X - axes.CenterX;
            var dy = point.Y - axes.CenterY;
            var t = dx * axes.AxisX + dy * axes.AxisY;
            var lateral = dx * axes.LateralX + dy * axes.LateralY;
            var sampleIndex = (int)Math.Round((t - minT) / majorLength * (sampleCount - 1));
            sampleIndex = Math.Clamp(sampleIndex, 0, sampleCount - 1);

            if (counts[sampleIndex] == 0)
            {
                profile[sampleIndex] = 0.0;
            }

            profile[sampleIndex] += lateral / majorLength;
            counts[sampleIndex]++;
        }

        for (var index = 0; index < profile.Length; index++)
        {
            if (counts[index] > 0)
            {
                profile[index] /= counts[index];
            }
        }

        FillMissingProfileValues(profile);
        RemoveLinearTrend(profile);
        SmoothProfile(profile);
        return profile;
    }

    private static double[] CreateVerticalProfile(Mat mask, int sampleCount)
    {
        using var binary = CreateBinaryMask(mask);
        var rect = FindForegroundRect(binary);
        if (rect.Width <= 0 || rect.Height <= 1)
        {
            return Array.Empty<double>();
        }

        using var cropped = new Mat(binary, rect);
        var profile = new double[sampleCount];
        Array.Fill(profile, double.NaN);
        var rowWindow = Math.Max(1, cropped.Rows / (sampleCount * 2));

        for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            var y = sampleCount == 1
                ? 0
                : (int)Math.Round(sampleIndex * (cropped.Rows - 1) / (double)(sampleCount - 1));
            var startY = Math.Max(0, y - rowWindow);
            var endY = Math.Min(cropped.Rows - 1, y + rowWindow);
            var xSum = 0.0;
            var count = 0;

            for (var row = startY; row <= endY; row++)
            {
                for (var x = 0; x < cropped.Cols; x++)
                {
                    if (cropped.At<byte>(row, x) == 0)
                    {
                        continue;
                    }

                    xSum += x;
                    count++;
                }
            }

            if (count > 0)
            {
                // Normalize lateral motion by feature height, so scale changes do not dominate shape similarity.
                profile[sampleIndex] = (xSum / count - (cropped.Cols - 1) / 2.0) / Math.Max(1.0, cropped.Rows - 1);
            }
        }

        FillMissingProfileValues(profile);
        RemoveLinearTrend(profile);
        SmoothProfile(profile);
        return profile;
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

    private static PrincipalAxes CalculatePrincipalAxes(IReadOnlyList<Point> points)
    {
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
            return PrincipalAxes.Invalid;
        }

        var angle = Math.Atan2(2.0 * covarianceXy, covarianceXx - covarianceYy) * 0.5;
        var axisX = Math.Cos(angle);
        var axisY = Math.Sin(angle);
        return new PrincipalAxes(
            meanX,
            meanY,
            axisX,
            axisY,
            -axisY,
            axisX,
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

    private static Rect FindForegroundRect(Mat binary)
    {
        Cv2.FindContours(
            binary,
            out Point[][] contours,
            out _,
            RetrievalModes.External,
            ContourApproximationModes.ApproxSimple);

        if (contours.Length == 0)
        {
            return new Rect();
        }

        var rect = Cv2.BoundingRect(contours[0]);
        for (var index = 1; index < contours.Length; index++)
        {
            rect = Union(rect, Cv2.BoundingRect(contours[index]));
        }

        return rect;
    }

    private static Rect Union(Rect first, Rect second)
    {
        var left = Math.Min(first.Left, second.Left);
        var top = Math.Min(first.Top, second.Top);
        var right = Math.Max(first.Right, second.Right);
        var bottom = Math.Max(first.Bottom, second.Bottom);
        return Rect.FromLTRB(left, top, right, bottom);
    }

    private static void FillMissingProfileValues(double[] profile)
    {
        var firstKnown = Array.FindIndex(profile, value => !double.IsNaN(value));
        if (firstKnown < 0)
        {
            return;
        }

        for (var index = 0; index < firstKnown; index++)
        {
            profile[index] = profile[firstKnown];
        }

        var previousKnown = firstKnown;
        for (var index = firstKnown + 1; index < profile.Length; index++)
        {
            if (!double.IsNaN(profile[index]))
            {
                InterpolateMissingRange(profile, previousKnown, index);
                previousKnown = index;
            }
        }

        for (var index = previousKnown + 1; index < profile.Length; index++)
        {
            profile[index] = profile[previousKnown];
        }
    }

    private static void InterpolateMissingRange(double[] profile, int startIndex, int endIndex)
    {
        var gap = endIndex - startIndex;
        if (gap <= 1)
        {
            return;
        }

        var startValue = profile[startIndex];
        var endValue = profile[endIndex];
        for (var index = startIndex + 1; index < endIndex; index++)
        {
            var t = (index - startIndex) / (double)gap;
            profile[index] = startValue + (endValue - startValue) * t;
        }
    }

    private static void RemoveLinearTrend(double[] profile)
    {
        if (profile.Length < 2)
        {
            return;
        }

        var first = profile[0];
        var last = profile[^1];
        for (var index = 0; index < profile.Length; index++)
        {
            var t = index / (double)(profile.Length - 1);
            profile[index] -= first + (last - first) * t;
        }

        var mean = profile.Average();
        for (var index = 0; index < profile.Length; index++)
        {
            profile[index] -= mean;
        }
    }

    private static void SmoothProfile(double[] profile)
    {
        if (profile.Length < 3)
        {
            return;
        }

        var copy = profile.ToArray();
        for (var index = 1; index < profile.Length - 1; index++)
        {
            profile[index] = (copy[index - 1] + copy[index] + copy[index + 1]) / 3.0;
        }
    }

    private static double MeanAbsoluteDistance(IReadOnlyList<double> first, IReadOnlyList<double> second)
    {
        var count = Math.Min(first.Count, second.Count);
        if (count == 0)
        {
            return double.MaxValue;
        }

        var sum = 0.0;
        for (var index = 0; index < count; index++)
        {
            sum += Math.Abs(first[index] - second[index]);
        }

        return sum / count;
    }

    private static void CompareCandidateProfile(
        IReadOnlyList<double> referenceProfile,
        IReadOnlyList<double> candidateProfile,
        string transform,
        ref double bestDistance,
        ref string bestTransform)
    {
        var distance = MeanAbsoluteDistance(referenceProfile, candidateProfile);
        if (distance >= bestDistance)
        {
            return;
        }

        bestDistance = distance;
        bestTransform = transform;
    }

    private static double[] ReverseProfile(IReadOnlyList<double> profile)
    {
        var reversed = profile.ToArray();
        Array.Reverse(reversed);
        return reversed;
    }

    private static double[] NegateProfile(IReadOnlyList<double> profile)
    {
        var negated = new double[profile.Count];
        for (var index = 0; index < profile.Count; index++)
        {
            negated[index] = -profile[index];
        }

        return negated;
    }

    private readonly record struct PrincipalAxes(
        double CenterX,
        double CenterY,
        double AxisX,
        double AxisY,
        double LateralX,
        double LateralY,
        bool IsValid)
    {
        public static PrincipalAxes Invalid { get; } = new(0, 0, 0, 0, 0, 0, IsValid: false);
    }
}

public readonly record struct ComponentShapeScoreResult(double Score, string Transform);

public readonly record struct ComponentShapeProfile(double[] Values)
{
    public static ComponentShapeProfile Empty { get; } = new(Array.Empty<double>());

    public bool IsValid => Values.Length > 0;
}
