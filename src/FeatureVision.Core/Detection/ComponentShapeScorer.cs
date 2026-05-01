using FeatureVision.Core.Models;
using OpenCvSharp;

namespace FeatureVision.Core.Detection;

public sealed class ComponentShapeScorer
{
    public double Score(Mat referenceMask, Mat candidateMask, DetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(referenceMask);
        ArgumentNullException.ThrowIfNull(candidateMask);
        ArgumentNullException.ThrowIfNull(settings);

        if (referenceMask.Empty() || candidateMask.Empty())
        {
            return 0.0;
        }

        var sampleCount = Math.Clamp(settings.ComponentShapeProfileSamples, 16, 256);
        var referenceProfile = CreateProfile(referenceMask, sampleCount);
        var candidateProfile = CreateProfile(candidateMask, sampleCount);
        if (referenceProfile.Length == 0 || candidateProfile.Length == 0)
        {
            return 0.0;
        }

        var distance = Math.Min(
            MeanAbsoluteDistance(referenceProfile, candidateProfile),
            MeanAbsoluteDistance(referenceProfile, ReverseProfile(candidateProfile)));

        var sensitivity = Math.Max(0.1, settings.ComponentShapeDistanceSensitivity);
        return Math.Clamp(Math.Exp(-distance * sensitivity), 0.0, 1.0);
    }

    private static double[] CreateProfile(Mat mask, int sampleCount)
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

    private static double[] ReverseProfile(IReadOnlyList<double> profile)
    {
        var reversed = profile.ToArray();
        Array.Reverse(reversed);
        return reversed;
    }
}
