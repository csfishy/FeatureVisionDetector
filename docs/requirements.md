# FeatureVision Requirements

## Purpose

FeatureVision is a C# .NET 8 WinForms system for creating feature annotation packages and using those packages for live camera detection. The system has two desktop applications and one shared library:

- `FeatureVision.AnnotationTool`
- `FeatureVision.RuntimeApp`
- `FeatureVision.Core`

The annotation app lets users mark target features on one or more sample images, creates binary masks, measures each marked feature, and saves a reusable feature package. The runtime app loads that package, captures live frames from a DirectShow camera, detects matching features, and draws measurement overlays.

## Primary Goals

- Let users open multiple sample images for annotation.
- Let users highlight target features using rectangle ROI, polygon selection, brush, and eraser tools.
- Generate one binary mask per marked feature.
- Compute feature center and rotation angle from each binary mask.
- Save a feature package containing `feature.json`, sample images, and mask images.
- Load the feature package in a runtime detection app.
- Capture live camera frames through OpenCvSharp using `VideoCaptureAPIs.DSHOW`.
- Detect target features in live frames using the package data.
- Output center X, center Y, rotation angle, matching score, and bounding box for each detection.
- Draw detection overlays on the live image.

## Solution Structure

The intended solution layout is:

```text
src/
  FeatureVision.Core/
  FeatureVision.AnnotationTool/
  FeatureVision.RuntimeApp/
docs/
  requirements.md
  feature-file-format.md
  development-plan.md
  test-checklist.md
AGENTS.md
```

## Technology Stack

- Language: C#
- Target framework: .NET 8
- UI: WinForms
- Image processing: OpenCvSharp4
- Camera backend: OpenCvSharp `VideoCapture`
- Camera API: `VideoCaptureAPIs.DSHOW`
- Feature package container: ZIP-based package with a project-specific extension
- Package manifest: JSON

## Shared Library Requirements

`FeatureVision.Core` must contain reusable, UI-independent logic.

Required model and service types:

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

### Core Responsibilities

- Represent annotated features and samples.
- Read and write versioned feature package files.
- Build binary masks from annotation geometry and brush strokes.
- Analyze masks to compute center, bounding box, contour, and rotation angle.
- Generate rotated templates from annotated feature samples.
- Match generated templates against runtime frames.
- Suppress duplicate detections with non-maximum suppression.
- Keep image-processing logic independent of WinForms controls.

### Core Non-Goals

- No WinForms UI code.
- No direct camera lifecycle ownership.
- No application-level dialogs or message boxes.
- No app-specific file picker behavior.

## Annotation Tool Requirements

`FeatureVision.AnnotationTool` is an operator-facing WinForms app for building feature packages.

### Image Loading

- Open one or more image files in a session.
- Display the active image at a usable zoom level.
- Preserve image aspect ratio while viewing.
- Let the user switch between loaded images.
- Track unsaved annotation changes.
- Handle unsupported or corrupt image files without crashing.

### Annotation Tools

The tool must support:

- Rectangle ROI selection.
- Polygon selection.
- Brush painting.
- Eraser painting.

The annotation result for each feature must become a binary mask where target pixels are foreground and all other pixels are background.

### Feature Management

- Create one or more marked features per sample image.
- Assign each feature a stable ID.
- Let the user select, inspect, rename, and delete features.
- Store the relationship between sample image, mask image, and feature metadata.
- Allow corrections before saving the package.

### Mask Generation

- Convert rectangle and polygon selections into filled binary mask regions.
- Apply brush strokes to add foreground pixels.
- Apply eraser strokes to remove foreground pixels.
- Save mask images using a lossless format.
- Keep mask dimensions aligned with the source sample image.

### Geometry Analysis

For each completed mask, compute:

- Center X.
- Center Y.
- Rotation angle.
- Bounding box.
- Area.
- Optional contour or polygon points when useful.

The rotation angle should be derived from mask geometry, such as contour principal axis, image moments, or `MinAreaRect`, and documented consistently so runtime output uses the same convention.

### Package Save

- Save the complete feature package as a single file.
- Include `feature.json`.
- Include sample images.
- Include binary mask images.
- Include detection settings and version metadata.
- Validate package consistency before writing.

## Runtime App Requirements

`FeatureVision.RuntimeApp` is a WinForms app for live detection.

### Package Loading

- Load a feature package created by the annotation tool.
- Validate manifest version and required package entries.
- Display enough package metadata for the user to confirm the correct feature is loaded.
- Handle missing, invalid, or incompatible packages without crashing.

### Camera Capture

- Capture live frames from a DirectShow-compatible camera.
- Use OpenCvSharp `VideoCapture` with `VideoCaptureAPIs.DSHOW`.
- Start and stop the camera without blocking the UI thread.
- Dispose camera and frame resources correctly.
- Surface camera open failures clearly.

### Live Detection

- Run package-based feature detection on live frames.
- Use generated templates and/or mask-derived descriptors from the feature package.
- Return one `DetectionResult` per accepted detection.
- Include these fields per detection:
  - Center X.
  - Center Y.
  - Rotation angle.
  - Matching score.
  - Bounding box.
- Apply non-maximum suppression to reduce duplicate detections.
- Allow detection settings to control thresholds, scale range, rotation range, and maximum detections.

### Overlay

- Draw bounding boxes on the live image.
- Draw center markers and angle indicators when useful.
- Display matching scores when useful for tuning.
- Keep overlay coordinates aligned with displayed preview coordinates.
- Keep rendering separate from matching logic.

## Detection Settings

`DetectionSettings` should include initial support for:

- Minimum score threshold.
- Rotation search range.
- Rotation search step.
- Scale search range.
- Scale search step.
- Non-maximum suppression overlap threshold.
- Maximum detections per frame.
- Optional ROI.
- Optional preprocessing options such as grayscale conversion, blur, thresholding, contrast normalization, or edge enhancement.

## Non-Functional Requirements

- The UI must remain responsive during annotation, camera preview, and live detection.
- Long-running camera loops must use `CancellationToken`.
- OpenCvSharp `Mat`, `Bitmap`, and other disposable image resources must be disposed correctly.
- Core logic should be deterministic for the same inputs and settings.
- File format should be versioned for future compatibility.
- Error messages should help the operator recover without exposing implementation details.
- The runtime app should avoid unbounded memory growth during extended live preview.
- The annotation app should avoid destructive edits to original source images unless explicitly requested.

## Assumptions

- The operating system is Windows.
- The camera is DirectShow-compatible.
- The user can provide representative sample images for annotation.
- Annotated features are visually distinctive enough for template or mask-based matching.
- The first implementation favors deterministic classical image processing over machine learning.

## Out of Scope for the First Implementation

- Neural network model training.
- Multi-camera synchronization.
- Remote web UI.
- Cloud storage.
- User account management.
- Persistent production database.
- Automatic annotation.
- Hardware triggering.

## Acceptance Criteria

- The annotation tool can open multiple images.
- The user can mark target features with rectangle, polygon, brush, and eraser tools.
- The annotation tool can generate one binary mask per marked feature.
- The annotation tool computes center and rotation angle from each mask.
- The annotation tool saves a package containing `feature.json`, sample images, and mask images.
- The runtime app can load that package.
- The runtime app can capture live frames from a DirectShow camera.
- The runtime app outputs center, angle, score, and bounding box for detected features.
- The runtime app draws detection overlays on live frames.
- UI responsiveness and memory behavior are acceptable during normal use.
