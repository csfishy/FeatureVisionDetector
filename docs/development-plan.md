# FeatureVision Development Plan

## Overview

Development should proceed in documentation-first, core-first phases. The annotation tool and runtime app both depend on `FeatureVision.Core`, so the shared package models, geometry analysis, and feature matching contracts should be established before large UI work begins.

The first milestone is documentation only:

- `docs/requirements.md`
- `docs/feature-file-format.md`
- `docs/development-plan.md`
- `docs/test-checklist.md`
- `AGENTS.md`

No production code should be added in this milestone.

## Guiding Principles

- Keep UI code, package I/O, geometry analysis, and runtime matching separate.
- Build reusable logic in `FeatureVision.Core` before wiring it into either app.
- Use OpenCvSharp for image processing and camera capture.
- Make the feature package format explicit and versioned from the beginning.
- Prefer deterministic classical image processing before considering learned models.
- Verify each stage with saved images and known masks before live-camera integration.

## Target Projects

### FeatureVision.Core

Shared class library containing model types, package read/write logic, mask building, mask geometry analysis, rotated template generation, template matching, and non-maximum suppression.

### FeatureVision.AnnotationTool

WinForms desktop app for opening sample images, marking features, generating masks, reviewing computed geometry, and saving feature packages.

### FeatureVision.RuntimeApp

WinForms desktop app for loading feature packages, capturing live DirectShow frames, detecting target features, and drawing overlays.

## Phase 0: Documentation and Format Definition

### Objective

Create a clear project baseline before implementation begins.

### Scope

- Document functional requirements.
- Define the feature package file format.
- Document implementation phases.
- Create the manual test checklist.
- Update `AGENTS.md` with repository-specific guidance.

### Deliverables

- Requirements document.
- Feature file format document.
- Development plan.
- Test checklist.
- Updated repository instructions.

### Exit Criteria

- The expected apps, core library, package format, and test scope are documented.
- No source code is implemented as part of this phase.

## Phase 1: Solution and Core Models

### Objective

Create the solution structure and shared model contracts without app-specific UI behavior.

### Scope

- Create `FeatureVision.Core`.
- Create `FeatureVision.AnnotationTool`.
- Create `FeatureVision.RuntimeApp`.
- Add OpenCvSharp dependencies where needed.
- Define model classes:
  - `FeatureModel`
  - `FeatureSample`
  - `FeatureFileManifest`
  - `DetectionSettings`
  - `DetectionResult`
- Define initial package read/write interfaces.

### Deliverables

- Buildable solution with three projects.
- Core model classes.
- Basic unit-test or console-test entry point for package model validation if tests are added.

### Exit Criteria

- Solution builds.
- Core models can represent one feature package with multiple samples and masks.
- No detection or annotation UI behavior is required yet.

## Phase 2: Feature Package Reader and Writer

### Objective

Implement stable package persistence before the annotation UI relies on it.

### Scope

- Implement `FeatureFileManifest`.
- Implement `FeatureFileReader`.
- Implement `FeatureFileWriter`.
- Save and load ZIP-based feature packages.
- Validate required package entries.
- Preserve manifest version metadata.
- Store sample images and mask images losslessly where possible.

### Deliverables

- Package writer that creates a valid package.
- Package reader that reconstructs feature metadata and image references.
- Validation errors for missing or malformed package entries.

### Exit Criteria

- A package can be written and read back with equivalent manifest data.
- Missing `feature.json`, missing sample images, and missing mask images produce clear errors.

## Phase 3: Mask Builder and Geometry Analyzer

### Objective

Implement the shared logic that converts annotations into masks and measurements.

### Scope

- Implement `MaskBuilder`.
- Support filled rectangle regions.
- Support filled polygon regions.
- Support brush strokes.
- Support eraser strokes.
- Implement `GeometryAnalyzer`.
- Compute mask area, center, bounding box, and rotation angle.
- Establish the angle convention used by both apps.

### Deliverables

- Binary mask generation from annotation operations.
- Geometry result extraction from binary masks.
- Repeatable test images or fixtures for known geometry.

### Exit Criteria

- Rectangle, polygon, brush, and eraser operations produce expected masks.
- Known masks produce expected center and rotation values within tolerance.

## Phase 4: Annotation Tool MVP

### Objective

Build the first usable annotation workflow.

### Scope

- Open multiple images.
- Display the active image.
- Switch between loaded images.
- Add annotation tool selection.
- Implement rectangle ROI marking.
- Implement polygon marking.
- Implement brush and eraser marking.
- Show current mask overlay.
- Create and manage feature entries.
- Display computed center and rotation angle.
- Save a feature package through `FeatureFileWriter`.

### Deliverables

- Launchable WinForms annotation app.
- Multi-image annotation workflow.
- Feature list and active feature editing.
- Package save workflow.

### Exit Criteria

- A user can create a package from sample images without editing source files manually.
- Saved package passes package validation.

## Phase 5: Template Generation and Matching Core

### Objective

Implement package-based detection logic in `FeatureVision.Core`.

### Scope

- Implement `RotatedTemplateGenerator`.
- Generate rotated and optionally scaled templates from sample masks or masked image regions.
- Implement `TemplateFeatureMatcher`.
- Match templates against input frames.
- Emit candidate `DetectionResult` objects.
- Implement `NonMaximumSuppression`.
- Suppress duplicate overlapping detections.
- Respect `DetectionSettings`.

### Deliverables

- Static-image detection API.
- Matching score output.
- Center, angle, and bounding box output.
- Duplicate suppression.

### Exit Criteria

- The matcher can detect annotated sample-like features in static images.
- Results include center X, center Y, rotation angle, matching score, and bounding box.
- Detection settings affect the matcher predictably.

## Phase 6: Runtime App MVP

### Objective

Build the live camera detection app around the validated core matcher.

### Scope

- Load a feature package.
- Open a DirectShow camera with OpenCvSharp.
- Show live preview.
- Run detection on live frames.
- Draw detection overlays.
- Show detection result values.
- Start and stop camera and detection loops without blocking the UI.

### Deliverables

- Launchable WinForms runtime app.
- Package load workflow.
- Live preview.
- Live detection overlay.
- Result output panel or table.

### Exit Criteria

- The app loads a package created by the annotation tool.
- Live camera preview works.
- Detected features are displayed with center, angle, score, and bounding box.
- UI remains responsive while detection runs.

## Phase 7: Tuning, Diagnostics, and Stability

### Objective

Make the system usable with real scenes and representative camera input.

### Scope

- Add detection settings controls.
- Add ROI support for runtime matching.
- Add debug visualization for masks, templates, candidates, and final detections.
- Measure frame rate and detection latency.
- Check memory behavior over extended runs.
- Improve error handling and recovery paths.

### Deliverables

- Runtime tuning UI.
- Diagnostic overlays or debug image exports.
- Stability notes.
- Recommended default settings.

### Exit Criteria

- The runtime app can run live preview and detection for at least 5 minutes without noticeable memory growth.
- Camera open and close behavior is reliable.
- Settings changes affect subsequent detection frames without restarting the app.

## Risks and Mitigations

### Package Format Drift

Mitigation: define `feature.json` versioning early and validate required fields on read.

### Annotation and Mask Misalignment

Mitigation: store original image dimensions, keep masks the same pixel size as source images, and test with known masks.

### Angle Convention Confusion

Mitigation: document the convention in the file format and use the same convention in `GeometryAnalyzer`, `RotatedTemplateGenerator`, and `DetectionResult`.

### Duplicate Runtime Detections

Mitigation: implement non-maximum suppression before building runtime UI polish.

### UI Freezing During Live Detection

Mitigation: keep capture and detection off the UI thread, use cancellation tokens, and copy or dispose frame data deliberately.

### OpenCV Resource Leaks

Mitigation: centralize ownership rules for `Mat` and `Bitmap` objects and include memory checks in the test checklist.

## Suggested Milestone Order

1. Finish documentation and package format.
2. Create solution and shared core models.
3. Implement package read/write.
4. Implement mask building and geometry analysis.
5. Build annotation MVP.
6. Implement static-image template matching.
7. Build runtime MVP with camera and overlay.
8. Add tuning, diagnostics, and stability improvements.
