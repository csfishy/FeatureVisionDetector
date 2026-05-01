using FeatureVisionDetector.WinForms.Models;
using FeatureVisionDetector.WinForms.Services;
using OpenCvSharp;

var samplePath = @"D:\Users\csfishy\Documents\GitHub\FeatureVisionDetector\docs\assets\sample.jpg";
var templatePath = @"D:\Users\csfishy\Documents\GitHub\FeatureVisionDetector\docs\assets\single-line-template-expected.png";

using var sample = Cv2.ImRead(samplePath, ImreadModes.Color);
using var templateImage = Cv2.ImRead(templatePath, ImreadModes.Color);

var service = new FeatureDetectionService();
var settings = new DetectionSettings();
settings.MinTemplateMatchScore = 0.30D;

Console.WriteLine($"sample_empty={sample.Empty()}");
Console.WriteLine($"template_empty={templateImage.Empty()}");

var template = service.TryCreateFeatureTemplate(templateImage);
Console.WriteLine($"template_created={template is not null}");

if (template is not null)
{
    Console.WriteLine(
        $"template_box={template.BoundingBox.X},{template.BoundingBox.Y},{template.BoundingBox.Width},{template.BoundingBox.Height} " +
        $"area={Math.Round(template.Area, 2)} ratio={Math.Round(template.AspectRatio, 2)}");

    var results = service.Detect(sample, settings, template);
    Console.WriteLine($"template_detect_count={results.Count}");

    foreach (var result in results)
    {
        Console.WriteLine(
            $"box={result.BoundingBox.X},{result.BoundingBox.Y},{result.BoundingBox.Width},{result.BoundingBox.Height} " +
            $"score={Math.Round(result.ShapeDistance ?? 0D, 3)}");
    }

    using var sampleMask = Preprocess(sample, settings);
    foreach (var scale in new[] { 0.35, 0.45, 0.55, 0.65, 0.75, 0.85, 1.0 })
    {
        var scaledWidth = Math.Max(1, (int)Math.Round(template.ProcessedImage.Width * scale));
        var scaledHeight = Math.Max(1, (int)Math.Round(template.ProcessedImage.Height * scale));
        using var scaledTemplate = new Mat();
        Cv2.Resize(
            template.ProcessedImage,
            scaledTemplate,
            new Size(scaledWidth, scaledHeight),
            0,
            0,
            InterpolationFlags.Nearest);

        if (scaledTemplate.Width >= sampleMask.Width || scaledTemplate.Height >= sampleMask.Height)
        {
            continue;
        }

        using var matchResult = new Mat();
        Cv2.MatchTemplate(sampleMask, scaledTemplate, matchResult, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(matchResult, out _, out var maxScore, out _, out var maxLocation);
        Console.WriteLine($"scale={scale:0.00} size={scaledWidth}x{scaledHeight} max={maxScore:0.000} at {maxLocation.X},{maxLocation.Y}");

        if (Math.Abs(scale - 0.55) < 0.001)
        {
            using var dilated = new Mat();
            using var localWindow = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(Math.Max(3, (scaledWidth / 3) | 1), Math.Max(3, (scaledHeight / 8) | 1)));
            Cv2.Dilate(matchResult, dilated, localWindow);
            var localCandidates = new List<(double Score, Rect Rect)>();
            for (var y = 0; y < matchResult.Rows; y++)
            {
                for (var x = 0; x < matchResult.Cols; x++)
                {
                    var score = matchResult.At<float>(y, x);
                    if (score < 0.35)
                    {
                        continue;
                    }

                    var localPeak = dilated.At<float>(y, x);
                    if (score < localPeak - 0.0001F)
                    {
                        continue;
                    }

                    localCandidates.Add((score, new Rect(x, y, scaledWidth, scaledHeight)));
                }
            }

            Console.WriteLine("scale055_candidates:");
            foreach (var candidate in localCandidates.OrderByDescending(candidate => candidate.Score).Take(20))
            {
                Console.WriteLine($"peak={candidate.Score:0.000} box={candidate.Rect.X},{candidate.Rect.Y},{candidate.Rect.Width},{candidate.Rect.Height}");
            }

            var probeRects = new[]
            {
                new Rect(503, 278, scaledWidth, scaledHeight),
                new Rect(514, 278, scaledWidth, scaledHeight),
                new Rect(525, 278, scaledWidth, scaledHeight),
                new Rect(536, 278, scaledWidth, scaledHeight),
                new Rect(547, 278, scaledWidth, scaledHeight)
            };

            foreach (var rect in probeRects)
            {
                using var probe = new Mat(sampleMask, rect);
                Cv2.FindContours(
                    probe,
                    out var contours,
                    out _,
                    RetrievalModes.External,
                    ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0)
                {
                    Console.WriteLine($"probe={rect.X},{rect.Y} no contours");
                    continue;
                }

                var bestContour = contours.OrderByDescending(contour => Cv2.ContourArea(contour)).First();
                var bestBounding = Cv2.BoundingRect(bestContour);
                var normalizedContour = bestContour
                    .Select(point => new Point(point.X - bestBounding.X, point.Y - bestBounding.Y))
                    .ToArray();
                var shapeDistance = Cv2.MatchShapes(normalizedContour, template.Contour, ShapeMatchModes.I1, 0);
                Console.WriteLine(
                    $"probe={rect.X},{rect.Y} bbox={bestBounding.X},{bestBounding.Y},{bestBounding.Width},{bestBounding.Height} " +
                    $"area={Cv2.ContourArea(bestContour):0.0} shape={shapeDistance:0.000}");
            }
        }

        if (Math.Abs(scale - 0.45) < 0.001)
        {
            using var dilated = new Mat();
            using var localWindow = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(Math.Max(3, (scaledWidth / 3) | 1), Math.Max(3, (scaledHeight / 8) | 1)));
            Cv2.Dilate(matchResult, dilated, localWindow);
            var localCandidates = new List<(double Score, Rect Rect)>();
            for (var y = 0; y < matchResult.Rows; y++)
            {
                for (var x = 0; x < matchResult.Cols; x++)
                {
                    var score = matchResult.At<float>(y, x);
                    if (score < 0.35)
                    {
                        continue;
                    }

                    var localPeak = dilated.At<float>(y, x);
                    if (score < localPeak - 0.0001F)
                    {
                        continue;
                    }

                    localCandidates.Add((score, new Rect(x, y, scaledWidth, scaledHeight)));
                }
            }

            Console.WriteLine("scale045_candidates:");
            foreach (var candidate in localCandidates.OrderByDescending(candidate => candidate.Score).Take(20))
            {
                Console.WriteLine($"peak={candidate.Score:0.000} box={candidate.Rect.X},{candidate.Rect.Y},{candidate.Rect.Width},{candidate.Rect.Height}");
            }
        }
    }

    template.Dispose();
}

static Mat Preprocess(Mat source, DetectionSettings settings)
{
    var grayscale = new Mat();
    var blurred = new Mat();
    var blackHat = new Mat();
    var thresholded = new Mat();
    var cleaned = new Mat();
    var blackHatKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(7, 25));
    var openKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(1, 3));
    var closeKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(1, 7));

    Cv2.CvtColor(source, grayscale, ColorConversionCodes.BGR2GRAY);
    Cv2.GaussianBlur(grayscale, blurred, new Size(5, 5), 0);
    Cv2.MorphologyEx(blurred, blackHat, MorphTypes.BlackHat, blackHatKernel);
    Cv2.Threshold(blackHat, thresholded, settings.ThresholdValue, 255, ThresholdTypes.Binary);
    Cv2.MorphologyEx(thresholded, cleaned, MorphTypes.Open, openKernel);
    Cv2.MorphologyEx(cleaned, cleaned, MorphTypes.Close, closeKernel);

    grayscale.Dispose();
    blurred.Dispose();
    blackHat.Dispose();
    thresholded.Dispose();
    blackHatKernel.Dispose();
    openKernel.Dispose();
    closeKernel.Dispose();

    return cleaned;
}
