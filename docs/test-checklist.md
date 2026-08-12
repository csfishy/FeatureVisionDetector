# FeatureVision Test Checklist

## Test Goal

Use this checklist to validate the annotation tool, feature package format, shared core library, and runtime live detection app. Record failed items with screenshots, input files, package files, settings, and steps to reproduce.

## Test Assets and Setup

- Windows machine with .NET 8 runtime or SDK.
- DirectShow-compatible camera.
- Multiple representative sample images.
- At least one sample image with a known target feature count.
- At least one sample image with no valid target features.
- At least one package created by `FeatureVision.AnnotationTool`.
- Optional challenging scenes:
  - Uneven lighting.
  - Low contrast features.
  - Rotated features.
  - Background clutter.
  - Features partially outside the frame.

## Package Format Tests

- [ ] Package file uses the project-approved extension.
- [ ] Package contains `feature.json`.
- [ ] Package contains all referenced sample images.
- [ ] Package contains all referenced mask images.
- [ ] `feature.json` includes format version metadata.
- [ ] `feature.json` includes feature metadata.
- [ ] `feature.json` includes sample metadata.
- [ ] `feature.json` includes mask references.
- [ ] Missing `feature.json` is reported clearly.
- [ ] Missing sample image is reported clearly.
- [ ] Missing mask image is reported clearly.
- [ ] Invalid JSON is reported clearly.
- [ ] Unsupported package version is reported clearly.
- [ ] Save and load round trip preserves sample IDs, feature IDs, centers, angles, and settings.

## Annotation Tool: Image Loading

- [ ] Application launches without crashing.
- [ ] User can open one image.
- [ ] User can open multiple images.
- [ ] User can switch between loaded images.
- [ ] Active image displays with correct aspect ratio.
- [ ] Unsupported image format is handled without crashing.
- [ ] Corrupt image file is handled without crashing.
- [ ] Unsaved annotation changes are indicated before closing or loading a new package.

## Annotation Tool: Rectangle ROI

- [ ] Rectangle tool can create a marked region.
- [ ] Rectangle can be drawn in different drag directions.
- [ ] Rectangle mask aligns with the displayed image pixels.
- [ ] Rectangle selection can be cancelled or corrected.
- [ ] Rectangle-generated mask is binary.

## Annotation Tool: Polygon Selection (Roadmap — Not Implemented)

Do not count this section toward current alpha acceptance. Enable these checks only after the annotation UI exposes a working polygon tool.

- [ ] Polygon tool can add multiple vertices.
- [ ] Polygon can be completed intentionally.
- [ ] Filled polygon mask matches the selected region.
- [ ] Polygon selection can be cancelled or corrected.
- [ ] Self-intersecting or invalid polygon input is handled safely.

## Annotation Tool: Brush and Eraser

- [ ] Brush adds foreground pixels to the active feature mask.
- [ ] Brush size can be adjusted if size controls are implemented.
- [ ] Eraser removes foreground pixels from the active feature mask.
- [ ] Brush and eraser strokes remain aligned at different zoom levels.
- [ ] Repeated painting does not degrade UI responsiveness.
- [ ] Mask remains binary after brush and eraser operations.

## Annotation Tool: Feature Management

- [ ] User can create a new feature on the active image.
- [ ] User can select an existing feature.
- [ ] User can rename a feature if renaming is implemented.
- [ ] User can delete a feature.
- [ ] Multiple features on one image remain independent.
- [ ] Features on different images remain independent.
- [ ] Feature IDs remain stable after save and reload.

## Core: MaskBuilder

- [ ] Rectangle input produces expected filled binary mask.
- [ ] Polygon input produces expected filled binary mask. *(Roadmap: `MaskBuilder.BuildFromPolygon` is currently unimplemented.)*
- [ ] Brush strokes add pixels at expected coordinates.
- [ ] Eraser strokes remove pixels at expected coordinates.
- [ ] Mask dimensions match source image dimensions.
- [ ] Empty mask is handled safely.

## Core: GeometryAnalyzer

- [ ] Center is correct for a simple rectangular mask.
- [ ] Center is correct for an asymmetric mask within tolerance.
- [ ] Bounding box encloses all foreground pixels.
- [ ] Area matches foreground pixel count.
- [ ] Rotation angle is correct for vertical, horizontal, and rotated masks within tolerance.
- [ ] Empty or tiny masks return a safe validation error.
- [ ] Angle convention matches the documentation.

## Annotation Tool: Package Save

- [ ] User can save a feature package.
- [ ] Saved package contains `feature.json`.
- [ ] Saved package contains sample images.
- [ ] Saved package contains mask images.
- [ ] Saved package can be reopened by the annotation tool.
- [ ] Saved package can be loaded by the runtime app.
- [ ] Feature center and rotation angle persist after reload.

## Runtime App: Package Loading

- [ ] Application launches without crashing.
- [ ] User can load a valid feature package.
- [ ] Package metadata is shown or otherwise confirmable.
- [ ] Invalid package is rejected with a clear message.
- [ ] Package with missing images is rejected with a clear message.
- [ ] Detection cannot start without a valid package, or the disabled state is clear.

## Runtime App: Camera Open and Close

- [ ] DirectShow camera can be opened.
- [ ] Live preview starts after camera open.
- [ ] Camera can be closed from the UI.
- [ ] Camera can be reopened after closing.
- [ ] No-camera condition is handled with a clear message.
- [ ] Camera open failure does not freeze the UI.
- [ ] Closing the app releases the camera.

## Runtime App: Live Preview

- [ ] Preview updates continuously.
- [ ] Preview aspect ratio is acceptable.
- [ ] Window resize does not break preview rendering.
- [ ] Minimize and restore does not break preview rendering.
- [ ] Preview continues when detection is disabled.
- [ ] UI remains responsive during live preview.

## Runtime App: Detection On and Off

- [ ] Detection can be enabled from the UI.
- [ ] Detection can be disabled from the UI.
- [ ] Repeated detection on/off toggling does not crash.
- [ ] Stale overlays are cleared or updated when detection is disabled.
- [ ] Preview remains visible while detection is disabled.
- [ ] Detection settings changes apply to subsequent frames.

## Runtime App: Detection Output

- [ ] Each detection outputs center X.
- [ ] Each detection outputs center Y.
- [ ] Each detection outputs rotation angle.
- [ ] Each detection outputs matching score.
- [ ] Each detection outputs bounding box.
- [ ] Detection count matches visible overlay count.
- [ ] Output values update as the feature moves.
- [ ] Detections are stable on a static scene.

## Runtime App: Overlay

- [ ] Bounding boxes align with detected features.
- [ ] Center markers align with detected centers if shown.
- [ ] Angle indicators align with detected rotation if shown.
- [ ] Matching score labels are readable if shown.
- [ ] Overlay remains aligned after window resize.
- [ ] Overlay does not obscure the preview excessively.

## Runtime App: Matching Quality

- [ ] Correct count is produced on known sample images after tuning.
- [ ] No-target images do not produce excessive false positives.
- [ ] Rotated target features are detected within the configured rotation range.
- [ ] Duplicate detections are reduced by non-maximum suppression.
- [ ] Low-score candidates are rejected by the score threshold.
- [ ] ROI, if enabled, excludes detections outside the selected region.

## Performance and Stability

- [ ] Annotation tool remains responsive while editing large masks.
- [ ] Runtime app remains responsive during live preview.
- [ ] Runtime app remains responsive during live detection.
- [ ] No UI freeze occurs during normal live preview.
- [ ] No UI freeze occurs while detection is enabled.
- [ ] Live preview runs for 5 minutes without noticeable memory growth.
- [ ] Live detection runs for 5 minutes without noticeable memory growth.
- [ ] Repeated package load operations do not leak memory noticeably.
- [ ] Repeated camera open and close operations do not leak memory noticeably.
- [ ] CPU usage remains acceptable for the target machine.

## Defect Logging Notes

For every failed test, record:

- Date and tester.
- App name and build version.
- Feature package file used.
- Input image or camera scene.
- Detection settings.
- Expected result.
- Actual result.
- Screenshot or saved frame when the failure is visual.
- Steps to reproduce.

## Minimum Acceptance Summary

- Annotation tool opens multiple images.
- Rectangle, brush, and eraser tools can create binary masks.
- Polygon annotation is explicitly deferred from current alpha acceptance.
- Feature center and rotation angle are computed from masks.
- Feature package save and load works.
- Runtime app loads the package.
- Runtime app opens and closes a DirectShow camera.
- Runtime app detects features in live frames.
- Runtime app outputs center, angle, score, and bounding box.
- Runtime app draws aligned overlays.
- Live preview and live detection do not freeze the UI or grow memory noticeably during 5-minute checks.
