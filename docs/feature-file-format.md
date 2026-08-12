# FeatureVision Feature File Format

## Overview

A FeatureVision package is a ZIP archive with the required `.fvfeature` extension. `.featurepkg` is not a supported alias. The extension, format name, and current format version are defined once in `FeaturePackageFormat` and used by both desktop applications and the shared reader/writer.

Every package contains:

- `feature.json`
- one source image per sample
- one binary mask image per sample

The archive remains inspectable with ordinary ZIP tools, but applications must treat every package and image as untrusted input.

## Version and identifiers

```json
{
  "formatName": "FeatureVision.FeaturePackage",
  "formatVersion": "1.0"
}
```

The current reader accepts version `1.0`. Other versions are rejected so compatibility changes are explicit.

## Package layout

The writer produces deterministic sample directories from each sample's list position and sanitized ID:

```text
feature.json
samples/
  0001-sample-0001/
    image.png
    masks/
      feature.png
  0002-sample-0002/
    image.jpg
    masks/
      feature.png
```

Rules:

- `feature.json` is at the archive root.
- Sample image and mask references are relative paths under `samples/`.
- Absolute paths and parent-directory traversal are rejected.
- Runtime loading must not depend on files outside the archive.
- A mask must have the same pixel dimensions as its source image before it is used.
- Writers preserve each source file's extension; PNG is recommended for lossless masks.

## `feature.json` structure

This example matches the current C# model:

```json
{
  "formatName": "FeatureVision.FeaturePackage",
  "formatVersion": "1.0",
  "createdUtc": "2026-05-01T00:00:00Z",
  "createdBy": "FeatureVision.AnnotationTool",
  "featureModel": {
    "id": "model-20260501000000",
    "name": "Annotated Feature",
    "description": "",
    "angleConvention": "degrees-clockwise-from-positive-x",
    "samples": [
      {
        "id": "sample-0001",
        "name": "sample",
        "imagePath": "samples/0001-sample-0001/image.png",
        "maskPath": "samples/0001-sample-0001/masks/feature.png",
        "imageSize": {
          "width": 1280,
          "height": 720
        },
        "center": {
          "x": 415.25,
          "y": 302.75
        },
        "rotationAngleDegrees": 87.5,
        "boundingBox": {
          "x": 398,
          "y": 260,
          "width": 34,
          "height": 88
        },
        "areaPixels": 1840
      }
    ]
  },
  "detectionSettings": {
    "scoreThreshold": 0.72,
    "angleMin": -30.0,
    "angleMax": 30.0,
    "angleStep": 2.0,
    "scaleMin": 0.9,
    "scaleMax": 1.1,
    "scaleStep": 0.05,
    "blurKernelSize": 3,
    "blackHatKernelSize": 11,
    "nmsOverlapThreshold": 0.35,
    "maximumDetections": 100
  }
}
```

Compatibility aliases such as `minimumScore` and `rotationMinDegrees` may be serialized by older callers because the current model exposes both names. New packages should use the primary names shown above.

## Required model data

Top level:

- `formatName`
- `formatVersion`
- `featureModel`
- `detectionSettings`

Feature model:

- `id`
- `name`
- `angleConvention`
- `samples`

Each sample:

- `id`
- `imagePath`
- `maskPath`
- positive `imageSize.width` and `imageSize.height`
- finite `center.x`, `center.y`, and `rotationAngleDegrees`
- non-negative, finite `areaPixels`
- a non-negative `boundingBox`

## Angle and coordinate conventions

The initial convention is `degrees-clockwise-from-positive-x`:

- coordinates use the image coordinate system;
- the origin is the top-left pixel;
- positive X points right and positive Y points down;
- positive angle values rotate clockwise;
- bounding boxes use integer pixel coordinates; and
- centers may use floating-point coordinates.

`GeometryAnalyzer`, `RotatedTemplateGenerator`, `TemplateFeatureMatcher`, and `DetectionResult` must use the same convention.

## Mask requirements

- Mask dimensions equal the corresponding sample image dimensions.
- Background pixels are `0`.
- Foreground pixels should be `255`; readers treat any non-zero value as foreground.
- Lossless PNG is recommended.

## Reader safety limits

The current reader applies limits before extraction:

- at most 256 samples;
- at most 513 archive entries;
- `feature.json` at most 1 MiB;
- each asset at most 128 MiB uncompressed;
- the complete archive at most 512 MiB uncompressed;
- supported relative paths under `samples/` only;
- extraction paths must remain within the caller-provided destination;
- package geometry must be finite and non-negative where required; and
- key detection parameters must stay within safe ranges.

Image dimensions and decoded image validity are checked when the application loads the extracted assets. These limits reduce archive and resource-exhaustion risk; they are not a substitute for keeping System.Drawing, OpenCvSharp, and the native OpenCV runtime patched.

## Writer behavior

The writer:

- requires a `.fvfeature` destination;
- validates the sample count;
- writes `feature.json` at the root;
- assigns deterministic archive paths based on sample order and ID;
- writes referenced images and masks into the archive; and
- never writes absolute local paths into the package manifest.

## Compatibility policy

- New optional fields may be added in a future minor version.
- Removing or changing required fields requires a new major format version.
- Readers reject unsupported versions rather than silently guessing.
- Unknown JSON fields may be ignored when they do not conflict with required data.
