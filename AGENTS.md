# AGENTS.md

## Project Overview

This project is a C# .NET 8 WinForms vision system for feature annotation and live feature detection.

The solution has two applications and one shared library:

- `FeatureVision.AnnotationTool`
  - Opens multiple sample images.
  - Lets users mark target features with rectangle ROI, brush, and eraser tools.
  - Polygon selection is planned but is not implemented in the current annotation UI.
  - Generates one binary mask per marked feature.
  - Computes feature center and rotation angle from each mask.
  - Saves a feature package containing `feature.json`, sample images, and mask images.

- `FeatureVision.RuntimeApp`
  - Loads a feature package.
  - Captures live frames from a DirectShow camera with OpenCvSharp.
  - Detects target features in live frames using the loaded package.
  - Outputs center X, center Y, rotation angle, matching score, and bounding box.
  - Draws detection overlays on the live image.

- `FeatureVision.Core`
  - Contains shared models, package I/O, mask building, geometry analysis, template generation, matching, and non-maximum suppression.

## Technology Stack

- Language: C#
- UI: WinForms
- Target: .NET 8
- Image processing: OpenCvSharp4
- Camera backend: OpenCvSharp `VideoCapture`
- Camera API: `VideoCaptureAPIs.DSHOW`
- Package format: ZIP-based `.fvfeature` file with JSON manifest and PNG assets

## Architecture

Use a modular structure:

- `FeatureVision.Core`
  - `FeatureModel`
  - `FeatureSample`
  - `FeatureFileManifest`
  - `DetectionSettings`
  - `DetectionResult`
  - `FeatureFileReader`
  - `FeatureFileWriter`
  - `MaskBuilder`
  - `GeometryAnalyzer`
  - `TemplateFeatureMatcher`
  - `RotatedTemplateGenerator`
  - `NonMaximumSuppression`

- `FeatureVision.AnnotationTool`
  - WinForms UI for image loading, annotation tools, feature management, mask preview, geometry review, and package save.
  - Should call into `FeatureVision.Core` for mask generation, geometry analysis, and package writing.

- `FeatureVision.RuntimeApp`
  - WinForms UI for package loading, camera preview, live detection, result display, and overlays.
  - Should call into `FeatureVision.Core` for package reading, template generation, matching, and non-maximum suppression.

## Coding Rules

- Keep UI logic separated from image-processing logic.
- Avoid putting mask generation, geometry analysis, package I/O, or detection code directly in WinForms forms.
- Dispose OpenCvSharp `Mat`, `Bitmap`, and other image resources properly.
- Avoid memory leaks in live image preview and annotation mask editing.
- Use `CancellationToken` for background camera and detection loops.
- Do not block the UI thread.
- Keep `FeatureVision.Core` independent of WinForms controls.
- Add comments for non-obvious image-processing parameters and angle conventions.
- Store package paths as relative paths inside the package, never absolute local machine paths.
- Prefer deterministic package output where practical so package diffs are understandable during development.

## Annotation Requirements

The annotation tool must:

1. Open multiple images.
2. Let users switch between loaded images.
3. Support rectangle ROI selection.
4. Treat polygon selection as a roadmap requirement; do not describe it as implemented until the annotation UI supports it end to end.
5. Support brush marking.
6. Support eraser correction.
7. Generate binary masks aligned to the source image dimensions.
8. Compute center, bounding box, area, and rotation angle from each mask.
9. Save a package containing `feature.json`, sample images, and mask images.

## Runtime Detection Requirements

The runtime app must:

1. Load a valid feature package.
2. Open and close a DirectShow-compatible camera.
3. Display live camera frames.
4. Detect package-defined target features in live frames.
5. Output center X, center Y, rotation angle, matching score, and bounding box.
6. Apply non-maximum suppression to reduce duplicate detections.
7. Draw aligned overlays on the live image.
8. Keep preview and detection responsive during extended runs.

## Feature Package Requirements

The package format is documented in `docs/feature-file-format.md`.

Minimum package contents:

- `feature.json`
- `samples/<sample-id>/image.png`
- `samples/<sample-id>/masks/<feature-id>.png`

Package validation must check:

- Manifest exists and parses.
- Format version is supported.
- Referenced sample images exist.
- Referenced mask images exist.
- Mask dimensions match sample image dimensions.
- Required numeric geometry fields are present and finite.

## Test Requirements

Maintain `docs/test-checklist.md`.

Manual tests must cover:

- Opening multiple images.
- Rectangle, brush, and eraser annotation tools.
- Polygon annotation remains a separate pending manual-test section until implemented.
- Binary mask generation.
- Feature center and rotation angle computation.
- Package save and load.
- Camera open and close.
- Detection on and off.
- Correct count on sample images after tuning.
- Runtime output values: center X, center Y, angle, score, and bounding box.
- Overlay alignment.
- No UI freeze during live preview.
- No noticeable memory growth during a 5-minute live preview and detection run.

## Current Milestone

The documentation baseline is complete and the repository is preparing its first alpha prerelease. Keep documentation aligned with implemented behavior, add regression tests for package and geometry changes, and do not present roadmap items as shipped features.
