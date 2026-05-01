using FeatureVision.Core.Models;
using OpenCvSharp;

namespace FeatureVision.Core.Geometry;

public sealed class GeometryAnalyzer
{
    public GeometryResult Analyze(Mat mask)
    {
        return AnalyzeMask(mask);
    }

    public GeometryResult AnalyzeMask(Mat mask)
    {
        ArgumentNullException.ThrowIfNull(mask);

        if (mask.Empty())
        {
            return new GeometryResult();
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
            return new GeometryResult();
        }

        var largestContourIndex = FindLargestContourIndex(contours);
        var largestContour = contours[largestContourIndex];
        var boundingRect = Cv2.BoundingRect(largestContour);

        using var largestContourMask = new Mat(binaryMask.Size(), MatType.CV_8UC1, Scalar.Black);
        Cv2.DrawContours(largestContourMask, contours, largestContourIndex, Scalar.White, -1);

        var area = Cv2.CountNonZero(largestContourMask);
        if (area == 0)
        {
            return new GeometryResult();
        }

        var moments = Cv2.Moments(largestContourMask, binaryImage: true);
        var centerX = moments.M00 == 0 ? boundingRect.X + boundingRect.Width / 2.0 : moments.M10 / moments.M00;
        var centerY = moments.M00 == 0 ? boundingRect.Y + boundingRect.Height / 2.0 : moments.M01 / moments.M00;
        var foregroundPoints = CollectForegroundPoints(largestContourMask);
        var angle = foregroundPoints.Length >= 2
            ? CalculatePcaAngle(foregroundPoints)
            : CalculateMinAreaRectAngle(largestContour);

        return new GeometryResult
        {
            CenterX = centerX,
            CenterY = centerY,
            RotationAngleDegrees = NormalizeAngle(angle),
            BoundingBox = new RoiRect
            {
                X = boundingRect.X,
                Y = boundingRect.Y,
                Width = boundingRect.Width,
                Height = boundingRect.Height
            },
            Area = area
        };
    }

    private static Mat CreateBinaryMask(Mat mask)
    {
        var grayscale = new Mat();
        if (mask.Channels() == 1)
        {
            mask.ConvertTo(grayscale, MatType.CV_8UC1);
        }
        else if (mask.Channels() == 4)
        {
            Cv2.CvtColor(mask, grayscale, ColorConversionCodes.BGRA2GRAY);
        }
        else
        {
            Cv2.CvtColor(mask, grayscale, ColorConversionCodes.BGR2GRAY);
        }

        var binary = new Mat();
        Cv2.Threshold(grayscale, binary, 0, 255, ThresholdTypes.Binary);
        grayscale.Dispose();
        return binary;
    }

    private static int FindLargestContourIndex(IReadOnlyList<Point[]> contours)
    {
        var largestIndex = 0;
        var largestArea = 0.0;

        for (var index = 0; index < contours.Count; index++)
        {
            var area = Cv2.ContourArea(contours[index]);
            if (area <= largestArea)
            {
                continue;
            }

            largestArea = area;
            largestIndex = index;
        }

        return largestIndex;
    }

    private static double CalculatePcaAngle(IReadOnlyList<Point> points)
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

        return Math.Atan2(2.0 * covarianceXy, covarianceXx - covarianceYy) * 0.5 * 180.0 / Math.PI;
    }

    private static Point[] CollectForegroundPoints(Mat binaryMask)
    {
        var points = new List<Point>();

        for (var y = 0; y < binaryMask.Rows; y++)
        {
            for (var x = 0; x < binaryMask.Cols; x++)
            {
                if (binaryMask.At<byte>(y, x) == 0)
                {
                    continue;
                }

                points.Add(new Point(x, y));
            }
        }

        return points.ToArray();
    }

    private static double CalculateMinAreaRectAngle(Point[] contour)
    {
        var rotatedRect = Cv2.MinAreaRect(contour);
        var angle = rotatedRect.Angle;

        if (rotatedRect.Size.Height > rotatedRect.Size.Width)
        {
            angle += 90.0f;
        }

        return angle;
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
