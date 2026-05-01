# FeatureVision Feature File Format

## Overview

A FeatureVision feature package is a single ZIP-based file containing:

- `feature.json`
- Sample images
- Binary mask images

The recommended extension is `.fvfeature`. The file remains a standard ZIP archive so it can be inspected with normal ZIP tools during development and support.

## Package Layout

Recommended package layout:

```text
feature.json
samples/
  sample-0001/
    image.png
    masks/
      feature-0001.png
      feature-0002.png
  sample-0002/
    image.png
    masks/
      feature-0003.png
```

Rules:

- `feature.json` must exist at the package root.
- Sample image paths must be relative to the package root.
- Mask image paths must be relative to the package root.
- Masks must have the same pixel width and height as their source sample image.
- Mask images must be binary, where foreground target pixels are non-zero and background pixels are zero.
- PNG is recommended for both sample images and masks.
- Original source image paths may be stored as metadata, but runtime loading must not depend on them.

## Versioning

The manifest must include a format version.

Initial version:

```json
{
  "formatName": "FeatureVision.FeaturePackage",
  "formatVersion": "1.0"
}
```

Readers should reject unsupported major versions. Readers may accept newer minor versions if all required fields for the supported version are present.

## feature.json Structure

Example:

```json
{
  "formatName": "FeatureVision.FeaturePackage",
  "formatVersion": "1.0",
  "createdUtc": "2026-05-01T00:00:00Z",
  "createdBy": "FeatureVision.AnnotationTool",
  "featureModel": {
    "id": "model-0001",
    "name": "Target Feature",
    "description": "",
    "angleConvention": "degrees-clockwise-from-positive-x",
    "samples": [
      {
        "id": "sample-0001",
        "imagePath": "samples/sample-0001/image.png",
        "width": 1280,
        "height": 720,
        "features": [
          {
            "id": "feature-0001",
            "name": "Feature 1",
            "maskPath": "samples/sample-0001/masks/feature-0001.png",
            "centerX": 415.25,
            "centerY": 302.75,
            "rotationAngleDegrees": 87.5,
            "boundingBox": {
              "x": 398,
              "y": 260,
              "width": 34,
              "height": 88
            },
            "area": 1840
          }
        ]
      }
    ]
  },
  "detectionSettings": {
    "minimumScore": 0.72,
    "rotationMinDegrees": -30.0,
    "rotationMaxDegrees": 30.0,
    "rotationStepDegrees": 2.0,
    "scaleMin": 0.9,
    "scaleMax": 1.1,
    "scaleStep": 0.05,
    "nmsOverlapThreshold": 0.35,
    "maximumDetections": 100
  }
}
```

## Required Manifest Fields

Top-level required fields:

- `formatName`
- `formatVersion`
- `featureModel`
- `detectionSettings`

`featureModel` required fields:

- `id`
- `name`
- `angleConvention`
- `samples`

Each sample required fields:

- `id`
- `imagePath`
- `width`
- `height`
- `features`

Each feature required fields:

- `id`
- `maskPath`
- `centerX`
- `centerY`
- `rotationAngleDegrees`
- `boundingBox`
- `area`

Each bounding box required fields:

- `x`
- `y`
- `width`
- `height`

## Detection Settings Fields

Initial `detectionSettings` fields:

- `minimumScore`
- `rotationMinDegrees`
- `rotationMaxDegrees`
- `rotationStepDegrees`
- `scaleMin`
- `scaleMax`
- `scaleStep`
- `nmsOverlapThreshold`
- `maximumDetections`

Optional future fields:

- `roi`
- `preprocessGrayscale`
- `preprocessBlurKernelSize`
- `preprocessNormalizeContrast`
- `templateMatchMethod`
- `pyramidLevels`

## Angle Convention

The initial package convention is:

```text
degrees-clockwise-from-positive-x
```

Meaning:

- Angles are stored in degrees.
- The image coordinate system is used.
- Positive X points right.
- Positive Y points down.
- Positive angle values rotate clockwise.

`GeometryAnalyzer`, `RotatedTemplateGenerator`, `TemplateFeatureMatcher`, and `DetectionResult` must use the same convention.

## Coordinate Convention

- Pixel coordinates are measured in source image coordinates.
- Origin is the top-left pixel.
- X increases to the right.
- Y increases downward.
- Bounding boxes use integer pixel coordinates.
- Centers may use floating-point coordinates.

## Mask Image Requirements

- Mask width must equal the source sample image width.
- Mask height must equal the source sample image height.
- Mask format should be 8-bit single-channel PNG.
- Background pixels must be `0`.
- Foreground target pixels should be `255`.
- Readers should treat any non-zero mask pixel as foreground.

## Package Validation Rules

A package is valid only if:

- The ZIP can be opened.
- `feature.json` exists and can be parsed.
- `formatName` is recognized.
- `formatVersion` is supported.
- Every sample image path exists in the package.
- Every mask image path exists in the package.
- Each mask has the same dimensions as its sample image.
- Required numeric fields are finite values.
- Width, height, and area values are greater than zero for non-empty features.
- Detection settings are within safe ranges.

## Reader Behavior

`FeatureFileReader` should:

- Open the package.
- Parse `feature.json`.
- Validate required entries.
- Load or expose sample image data.
- Load or expose mask image data.
- Return a `FeatureModel` and `DetectionSettings`.
- Provide useful validation errors without crashing the caller.

## Writer Behavior

`FeatureFileWriter` should:

- Validate the model before writing.
- Write `feature.json` at the package root.
- Write all sample images.
- Write all mask images.
- Prefer deterministic entry names when possible.
- Avoid absolute paths inside the package.
- Avoid depending on files outside the package after save.

## Compatibility Notes

- Runtime apps should not require annotation-only metadata to perform detection.
- New optional fields may be added in minor versions.
- Renaming or removing required fields should require a major version change.
- Unknown fields should be ignored by readers unless they conflict with required fields.
