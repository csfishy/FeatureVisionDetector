using FeatureVision.Core.Models;

namespace FeatureVision.Core.Matching;

public sealed class NonMaximumSuppression
{
    public IReadOnlyList<DetectionResult> Suppress(
        IEnumerable<DetectionResult> detections,
        double overlapThreshold)
    {
        ArgumentNullException.ThrowIfNull(detections);

        var orderedDetections = detections
            .OrderByDescending(detection => detection.MatchingScore)
            .ToList();

        var acceptedDetections = new List<DetectionResult>();
        foreach (var detection in orderedDetections)
        {
            if (acceptedDetections.Any(accepted => CalculateIntersectionOverUnion(detection.BoundingBox, accepted.BoundingBox) > overlapThreshold))
            {
                continue;
            }

            acceptedDetections.Add(detection);
        }

        return acceptedDetections;
    }

    private static double CalculateIntersectionOverUnion(RoiRect first, RoiRect second)
    {
        var intersectionLeft = Math.Max(first.X, second.X);
        var intersectionTop = Math.Max(first.Y, second.Y);
        var intersectionRight = Math.Min(first.X + first.Width, second.X + second.Width);
        var intersectionBottom = Math.Min(first.Y + first.Height, second.Y + second.Height);

        var intersectionWidth = Math.Max(0, intersectionRight - intersectionLeft);
        var intersectionHeight = Math.Max(0, intersectionBottom - intersectionTop);
        var intersectionArea = intersectionWidth * intersectionHeight;
        if (intersectionArea == 0)
        {
            return 0.0;
        }

        var firstArea = Math.Max(0, first.Width) * Math.Max(0, first.Height);
        var secondArea = Math.Max(0, second.Width) * Math.Max(0, second.Height);
        var unionArea = firstArea + secondArea - intersectionArea;

        return unionArea <= 0 ? 0.0 : intersectionArea / (double)unionArea;
    }
}
