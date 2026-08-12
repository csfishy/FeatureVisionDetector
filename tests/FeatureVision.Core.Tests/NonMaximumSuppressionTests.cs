using FeatureVision.Core.Matching;
using FeatureVision.Core.Models;

namespace FeatureVision.Core.Tests;

public sealed class NonMaximumSuppressionTests
{
    [Fact]
    public void SuppressKeepsHighestScoreAndNonOverlappingResults()
    {
        var highest = Detection(0.95, 0, 0, 20, 20);
        var duplicate = Detection(0.80, 2, 2, 20, 20);
        var separate = Detection(0.75, 50, 50, 10, 10);

        var result = new NonMaximumSuppression().Suppress(
            [duplicate, separate, highest],
            overlapThreshold: 0.35);

        Assert.Equal(2, result.Count);
        Assert.Same(highest, result[0]);
        Assert.Contains(separate, result);
        Assert.DoesNotContain(duplicate, result);
    }

    private static DetectionResult Detection(
        double score,
        int x,
        int y,
        int width,
        int height)
    {
        return new DetectionResult
        {
            MatchingScore = score,
            BoundingBox = new RoiRect
            {
                X = x,
                Y = y,
                Width = width,
                Height = height
            }
        };
    }
}
