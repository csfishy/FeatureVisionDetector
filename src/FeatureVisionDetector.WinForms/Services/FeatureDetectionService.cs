using FeatureVisionDetector.WinForms.Models;
using OpenCvSharp;
using CvPoint = OpenCvSharp.Point;
using Rectangle = System.Drawing.Rectangle;
using Size = OpenCvSharp.Size;

namespace FeatureVisionDetector.WinForms.Services;

public sealed class FeatureDetectionService
{
    public List<FeatureResult> Detect(Mat source, DetectionSettings settings)
    {
        return Detect(source, settings, null);
    }

    public List<FeatureResult> Detect(Mat source, DetectionSettings settings, FeatureTemplate? featureTemplate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(settings);

        if (source.Empty())
        {
            return [];
        }

        var roi = GetProcessingRoi(source.Size(), settings.Roi);
        var offsetX = roi?.X ?? 0;
        var offsetY = roi?.Y ?? 0;

        using var roiSource = roi.HasValue ? new Mat(source, roi.Value) : null;
        var workingSource = roiSource ?? source;
        using var grayscale = new Mat();
        using var blurred = new Mat();
        using var blackHat = new Mat();
        using var thresholded = new Mat();
        using var cleaned = new Mat();
        using var blackHatKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(7, 25));
        using var openKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(1, 3));
        using var closeKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(1, 7));
        
        Cv2.CvtColor(workingSource, grayscale, ColorConversionCodes.BGR2GRAY);
        Cv2.GaussianBlur(grayscale, blurred, new Size(5, 5), 0);
        Cv2.MorphologyEx(blurred, blackHat, MorphTypes.BlackHat, blackHatKernel);
        Cv2.Threshold(blackHat, thresholded, settings.ThresholdValue, 255, ThresholdTypes.Binary);
        Cv2.MorphologyEx(thresholded, cleaned, MorphTypes.Open, openKernel);
        Cv2.MorphologyEx(cleaned, cleaned, MorphTypes.Close, closeKernel);

        if (featureTemplate is not null)
        {
            return DetectWithTemplate(cleaned, settings, featureTemplate, offsetX, offsetY);
        }

        return DetectFromContours(cleaned, settings, offsetX, offsetY, null);
    }

    public FeatureTemplate? TryCreateFeatureTemplate(Mat source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Empty())
        {
            return null;
        }

        using var grayscale = new Mat();
        using var blurred = new Mat();
        using var blackHat = new Mat();
        using var thresholded = new Mat();
        using var cleaned = new Mat();
        using var blackHatKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(7, 25));
        using var cleanupKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(1, 3));

        Cv2.CvtColor(source, grayscale, ColorConversionCodes.BGR2GRAY);
        Cv2.GaussianBlur(grayscale, blurred, new Size(3, 3), 0);
        Cv2.MorphologyEx(blurred, blackHat, MorphTypes.BlackHat, blackHatKernel);
        Cv2.Threshold(blackHat, thresholded, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
        Cv2.MorphologyEx(thresholded, cleaned, MorphTypes.Open, cleanupKernel);

        var bestContour = TryGetBestContour(cleaned, out var boundingRect, out var area);
        if (bestContour is null || boundingRect.Width <= 0 || boundingRect.Height <= 0)
        {
            return null;
        }

        var normalizedContour = bestContour
            .Select(point => new CvPoint(point.X - boundingRect.X, point.Y - boundingRect.Y))
            .ToArray();

        using var processedImage = new Mat(cleaned, boundingRect);
        var profile = TryBuildNormalizedProfile(processedImage);
        if (profile is null)
        {
            return null;
        }

        using var matchingTemplate = RenderTemplateMaskFromProfile(profile, processedImage.Height, processedImage.Width);

        var adjustedRect = new Rectangle(
            boundingRect.X,
            boundingRect.Y,
            boundingRect.Width,
            boundingRect.Height);

        var aspectRatio = boundingRect.Height / (double)Math.Max(boundingRect.Width, 1);
        return new FeatureTemplate(normalizedContour, adjustedRect, area, aspectRatio, matchingTemplate, profile);
    }

    private static List<FeatureResult> DetectFromContours(
        Mat cleaned,
        DetectionSettings settings,
        int offsetX,
        int offsetY,
        FeatureTemplate? featureTemplate,
        double? templateScore = null)
    {
        Cv2.FindContours(
            cleaned,
            out var contours,
            out _,
            RetrievalModes.External,
            ContourApproximationModes.ApproxSimple);

        var results = new List<FeatureResult>(contours.Length);

        foreach (var contour in contours)
        {
            var area = Cv2.ContourArea(contour);
            if (area < settings.MinArea || area > settings.MaxArea)
            {
                continue;
            }

            var boundingRect = Cv2.BoundingRect(contour);
            if (boundingRect.Width < settings.MinWidth || boundingRect.Width > settings.MaxWidth)
            {
                continue;
            }

            if (boundingRect.Height < settings.MinHeight)
            {
                continue;
            }

            var aspectRatio = boundingRect.Height / (double)Math.Max(boundingRect.Width, 1);
            if (aspectRatio < settings.MinAspectRatio)
            {
                continue;
            }

            double? shapeDistance = null;
            if (featureTemplate is not null
                && !IsTemplateMatch(
                    contour,
                    boundingRect,
                    cleaned,
                    featureTemplate,
                    new OpenCvSharp.Size(featureTemplate.BoundingBox.Width, featureTemplate.BoundingBox.Height),
                    settings,
                    out shapeDistance))
            {
                continue;
            }

            var adjustedRect = new Rectangle(
                x: boundingRect.X + offsetX,
                y: boundingRect.Y + offsetY,
                width: boundingRect.Width,
                height: boundingRect.Height);

            results.Add(new FeatureResult(adjustedRect, area, aspectRatio, templateScore ?? shapeDistance));
        }

        return results
            .OrderBy(result => result.BoundingBox.X)
            .ThenBy(result => result.BoundingBox.Y)
            .ToList();
    }

    private static List<FeatureResult> DetectWithTemplate(
        Mat cleaned,
        DetectionSettings settings,
        FeatureTemplate featureTemplate,
        int offsetX,
        int offsetY)
    {
        var candidates = CollectTemplateCandidates(cleaned, featureTemplate, settings);
        if (candidates.Count == 0)
        {
            return [];
        }

        var results = new List<FeatureResult>(candidates.Count);
        foreach (var candidate in candidates)
        {
            using var candidateMask = new Mat(cleaned, candidate.SourceRect);
            var result = CreateResultFromCandidate(candidateMask, candidate, featureTemplate, settings, offsetX, offsetY);
            if (result is not null)
            {
                results.Add(result);
            }
        }

        return results
            .OrderBy(result => result.BoundingBox.X)
            .ThenBy(result => result.BoundingBox.Y)
            .ToList();
    }

    private static List<TemplateCandidate> CollectTemplateCandidates(
        Mat cleaned,
        FeatureTemplate featureTemplate,
        DetectionSettings settings)
    {
        var allCandidates = new List<TemplateCandidate>();

        foreach (var scale in EnumerateTemplateScales(cleaned.Size(), featureTemplate.ProcessedImage.Size()))
        {
            var scaledWidth = Math.Max(1, (int)Math.Round(featureTemplate.ProcessedImage.Width * scale));
            var scaledHeight = Math.Max(1, (int)Math.Round(featureTemplate.ProcessedImage.Height * scale));
            if (scaledWidth >= cleaned.Width || scaledHeight >= cleaned.Height)
            {
                continue;
            }

            using var scaledTemplate = new Mat();
            Cv2.Resize(
                featureTemplate.ProcessedImage,
                scaledTemplate,
                new Size(scaledWidth, scaledHeight),
                0,
                0,
                InterpolationFlags.Nearest);

            using var matchResult = new Mat();
            Cv2.MatchTemplate(cleaned, scaledTemplate, matchResult, TemplateMatchModes.CCoeffNormed);

            allCandidates.AddRange(
                CollectCandidatesForScale(
                    matchResult,
                    scaledTemplate.Size(),
                    featureTemplate.ProcessedImage.Size(),
                    settings,
                    scale));
        }

        var filtered = new List<TemplateCandidate>();
        foreach (var candidate in allCandidates.OrderByDescending(candidate => candidate.Score))
        {
            if (filtered.Any(existing => IsNearDuplicate(existing.SourceRect, candidate.SourceRect)))
            {
                continue;
            }

            filtered.Add(candidate);
        }

        return filtered;
    }

    private static FeatureResult? CreateResultFromCandidate(
        Mat candidateMask,
        TemplateCandidate candidate,
        FeatureTemplate featureTemplate,
        DetectionSettings settings,
        int offsetX,
        int offsetY)
    {
        var bestContour = TryGetBestContour(candidateMask, out var boundingRect, out var area);
        if (bestContour is null)
        {
            return null;
        }

        if (!IsTemplateMatch(
                bestContour,
                boundingRect,
                candidateMask,
                featureTemplate,
                candidate.SourceRect.Size,
                settings,
                out _))
        {
            return null;
        }

        var aspectRatio = boundingRect.Height / (double)Math.Max(boundingRect.Width, 1);
        var passesGeometry = area >= settings.MinArea
            && area <= settings.MaxArea
            && boundingRect.Width >= settings.MinWidth
            && boundingRect.Width <= settings.MaxWidth
            && boundingRect.Height >= settings.MinHeight
            && aspectRatio >= settings.MinAspectRatio;
        var contourRepresentsWholeFeature =
            boundingRect.Height >= candidate.SourceRect.Height * 0.35D
            && boundingRect.Width >= Math.Max(1D, candidate.SourceRect.Width * 0.2D);

        if (passesGeometry && contourRepresentsWholeFeature)
        {
            return new FeatureResult(
                new Rectangle(
                    candidate.SourceRect.X + boundingRect.X + offsetX,
                    candidate.SourceRect.Y + boundingRect.Y + offsetY,
                    boundingRect.Width,
                    boundingRect.Height),
                area,
                aspectRatio,
                candidate.Score);
        }

        var candidateAspectRatio = candidate.SourceRect.Height / (double)Math.Max(candidate.SourceRect.Width, 1);
        return new FeatureResult(
            new Rectangle(
                candidate.SourceRect.X + offsetX,
                candidate.SourceRect.Y + offsetY,
                candidate.SourceRect.Width,
                candidate.SourceRect.Height),
            candidate.SourceRect.Width * candidate.SourceRect.Height,
            candidateAspectRatio,
            candidate.Score);
    }

    private static OpenCvSharp.Rect? GetProcessingRoi(OpenCvSharp.Size sourceSize, Rectangle? requestedRoi)
    {
        if (!requestedRoi.HasValue)
        {
            return null;
        }

        var sourceBounds = new Rectangle(0, 0, sourceSize.Width, sourceSize.Height);
        var clamped = Rectangle.Intersect(sourceBounds, requestedRoi.Value);

        if (clamped.Width <= 0 || clamped.Height <= 0)
        {
            return null;
        }

        return new OpenCvSharp.Rect(clamped.X, clamped.Y, clamped.Width, clamped.Height);
    }

    private static CvPoint[]? TryGetBestContour(Mat mask, out OpenCvSharp.Rect boundingRect, out double area)
    {
        Cv2.FindContours(
            mask,
            out var contours,
            out _,
            RetrievalModes.External,
            ContourApproximationModes.ApproxSimple);

        boundingRect = default;
        area = 0D;

        CvPoint[]? bestContour = null;
        foreach (var contour in contours)
        {
            var contourArea = Cv2.ContourArea(contour);
            if (contourArea <= area)
            {
                continue;
            }

            bestContour = contour;
            area = contourArea;
            boundingRect = Cv2.BoundingRect(contour);
        }

        return bestContour;
    }

    private static bool IsTemplateMatch(
        IReadOnlyList<CvPoint> contour,
        OpenCvSharp.Rect boundingRect,
        Mat candidateMask,
        FeatureTemplate featureTemplate,
        OpenCvSharp.Size expectedTemplateSize,
        DetectionSettings settings,
        out double? shapeDistance)
    {
        var templateHeight = Math.Max(expectedTemplateSize.Height, 1);
        var templateWidth = Math.Max(expectedTemplateSize.Width, 1);
        var heightRatio = boundingRect.Height / (double)templateHeight;
        var widthRatio = boundingRect.Width / (double)templateWidth;

        if (heightRatio < settings.MinTemplateHeightRatio || heightRatio > settings.MaxTemplateHeightRatio)
        {
            shapeDistance = null;
            return false;
        }

        if (widthRatio < settings.MinTemplateWidthRatio || widthRatio > settings.MaxTemplateWidthRatio)
        {
            shapeDistance = null;
            return false;
        }

        using var contourMask = new Mat(candidateMask, boundingRect);
        var candidateProfile = TryBuildNormalizedProfile(contourMask);
        if (candidateProfile is null)
        {
            shapeDistance = null;
            return false;
        }

        shapeDistance = CalculateProfileDistance(candidateProfile, featureTemplate.CenterlineProfile);

        return shapeDistance <= settings.MaxShapeDistance;
    }

    private static bool IsNearDuplicate(OpenCvSharp.Rect existing, OpenCvSharp.Rect candidate)
    {
        var centerDeltaX = Math.Abs((existing.X + existing.Width / 2D) - (candidate.X + candidate.Width / 2D));
        var centerDeltaY = Math.Abs((existing.Y + existing.Height / 2D) - (candidate.Y + candidate.Height / 2D));
        var widthLimit = Math.Max(Math.Min(existing.Width, candidate.Width) * 0.45D, 3D);
        var heightLimit = Math.Max(Math.Min(existing.Height, candidate.Height) * 0.18D, 5D);

        return centerDeltaX < widthLimit && centerDeltaY < heightLimit;
    }

    private static IEnumerable<double> EnumerateTemplateScales(OpenCvSharp.Size imageSize, OpenCvSharp.Size templateSize)
    {
        var minScale = Math.Max(0.3D, 6D / Math.Max(templateSize.Width, 1));
        var maxScale = Math.Min(
            1.1D,
            Math.Min(
                (imageSize.Width - 1D) / Math.Max(templateSize.Width, 1),
                (imageSize.Height - 1D) / Math.Max(templateSize.Height, 1)));

        for (var scale = minScale; scale <= maxScale + 0.0001D; scale += 0.05D)
        {
            yield return Math.Round(scale, 2);
        }
    }

    private static List<TemplateCandidate> CollectCandidatesForScale(
        Mat matchResult,
        OpenCvSharp.Size scaledTemplateSize,
        OpenCvSharp.Size baseTemplateSize,
        DetectionSettings settings,
        double scale)
    {
        Cv2.MinMaxLoc(matchResult, out _, out var maxScore, out _, out _);
        if (maxScore < settings.MinTemplateMatchScore)
        {
            return [];
        }

        var dynamicThreshold = Math.Max(settings.MinTemplateMatchScore, maxScore * 0.78D);

        using var dilated = new Mat();
        var localWindowWidth = Math.Max(3, (scaledTemplateSize.Width / 3) | 1);
        var localWindowHeight = Math.Max(3, (scaledTemplateSize.Height / 8) | 1);
        using var localWindow = Cv2.GetStructuringElement(
            MorphShapes.Rect,
            new Size(localWindowWidth, localWindowHeight));
        Cv2.Dilate(matchResult, dilated, localWindow);

        var candidates = new List<TemplateCandidate>();
        for (var y = 0; y < matchResult.Rows; y++)
        {
            for (var x = 0; x < matchResult.Cols; x++)
            {
                var score = matchResult.At<float>(y, x);
                if (score < dynamicThreshold)
                {
                    continue;
                }

                var localPeak = dilated.At<float>(y, x);
                if (score < localPeak - 0.0001F)
                {
                    continue;
                }

                candidates.Add(new TemplateCandidate(
                    score,
                    new OpenCvSharp.Rect(x, y, scaledTemplateSize.Width, scaledTemplateSize.Height),
                    scale));
            }
        }

        return candidates;
    }

    private static double[]? TryBuildNormalizedProfile(Mat mask, int sampleCount = 48)
    {
        var rowSamples = new List<(double Y, double X)>();
        for (var y = 0; y < mask.Rows; y++)
        {
            var count = 0;
            var sumX = 0D;

            for (var x = 0; x < mask.Cols; x++)
            {
                if (mask.At<byte>(y, x) == 0)
                {
                    continue;
                }

                count++;
                sumX += x;
            }

            if (count > 0)
            {
                rowSamples.Add((y, sumX / count));
            }
        }

        if (rowSamples.Count < Math.Max(8, sampleCount / 4))
        {
            return null;
        }

        var firstY = rowSamples[0].Y;
        var lastY = rowSamples[^1].Y;
        var yRange = Math.Max(1D, lastY - firstY);
        var profile = new double[sampleCount];

        for (var i = 0; i < sampleCount; i++)
        {
            var targetY = firstY + (yRange * i / Math.Max(sampleCount - 1D, 1D));
            profile[i] = InterpolateProfileSample(rowSamples, targetY);
        }

        var mean = profile.Average();
        var scale = Math.Max(mask.Cols - 1D, 1D);
        for (var i = 0; i < profile.Length; i++)
        {
            profile[i] = (profile[i] - mean) / scale;
        }

        return profile;
    }

    private static double InterpolateProfileSample(IReadOnlyList<(double Y, double X)> rowSamples, double targetY)
    {
        if (targetY <= rowSamples[0].Y)
        {
            return rowSamples[0].X;
        }

        if (targetY >= rowSamples[^1].Y)
        {
            return rowSamples[^1].X;
        }

        for (var i = 1; i < rowSamples.Count; i++)
        {
            if (targetY > rowSamples[i].Y)
            {
                continue;
            }

            var previous = rowSamples[i - 1];
            var next = rowSamples[i];
            var span = Math.Max(0.0001D, next.Y - previous.Y);
            var t = (targetY - previous.Y) / span;
            return previous.X + ((next.X - previous.X) * t);
        }

        return rowSamples[^1].X;
    }

    private static double CalculateProfileDistance(
        IReadOnlyList<double> candidateProfile,
        IReadOnlyList<double> templateProfile)
    {
        var count = Math.Min(candidateProfile.Count, templateProfile.Count);
        if (count == 0)
        {
            return double.MaxValue;
        }

        var total = 0D;
        for (var i = 0; i < count; i++)
        {
            total += Math.Abs(candidateProfile[i] - templateProfile[i]);
        }

        return total / count;
    }

    private static Mat RenderTemplateMaskFromProfile(
        IReadOnlyList<double> profile,
        int sourceHeight,
        int sourceWidth)
    {
        var templateHeight = Math.Max(sourceHeight, profile.Count);
        var templateWidth = Math.Max(9, Math.Min(16, (int)Math.Round(sourceWidth * 0.35D)));
        var mask = new Mat(templateHeight, templateWidth, MatType.CV_8UC1, Scalar.Black);

        var centerX = (templateWidth - 1) / 2D;
        var points = new CvPoint[profile.Count];
        for (var i = 0; i < profile.Count; i++)
        {
            var x = (int)Math.Round(centerX + (profile[i] * (templateWidth - 1) * 1.4D));
            var y = (int)Math.Round(i * (templateHeight - 1D) / Math.Max(profile.Count - 1D, 1D));
            x = Math.Clamp(x, 0, templateWidth - 1);
            y = Math.Clamp(y, 0, templateHeight - 1);
            points[i] = new CvPoint(x, y);
        }

        Cv2.Polylines(mask, [points], false, Scalar.White, 2, LineTypes.AntiAlias);
        return mask;
    }

    private sealed record TemplateCandidate(double Score, OpenCvSharp.Rect SourceRect, double Scale);
}
