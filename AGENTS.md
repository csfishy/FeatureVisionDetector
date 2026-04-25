# AGENTS.md

## Project Overview

This project is a C# WinForms vision application for detecting thin dark line features from a live camera image.

The application must:
- Capture live video using DirectShow-compatible camera input.
- Display the live camera image in a WinForms UI.
- Enable or disable feature detection using a button.
- Detect thin, dark, vertical or near-vertical line features.
- Draw bounding boxes around each detected feature.
- Display the total feature count in real time.

## Technology Stack

- Language: C#
- UI: WinForms
- Target: .NET 8 WinForms unless otherwise specified
- Image processing: OpenCvSharp4
- Camera backend: OpenCvSharp VideoCapture with VideoCaptureAPIs.DSHOW

## Architecture

Use a modular structure:

- CameraService
  - Opens and closes the camera.
  - Captures frames from DirectShow.
  - Exposes the latest frame to the UI loop.

- FeatureDetectionService
  - Converts frames to grayscale.
  - Enhances dark thin features.
  - Performs thresholding and morphology.
  - Finds contours or connected components.
  - Filters by area, width, height, aspect ratio, and ROI.
  - Returns a list of FeatureResult objects.

- OverlayRenderer
  - Draws bounding boxes, labels, and count text.
  - Does not perform detection logic.

- MainForm
  - Handles UI events.
  - Starts and stops camera preview.
  - Toggles detection.
  - Displays live image and count.

## Coding Rules

- Keep UI logic separated from image-processing logic.
- Avoid putting detection code directly in MainForm.
- Dispose OpenCvSharp Mat and Bitmap objects properly.
- Avoid memory leaks in live image preview.
- Use CancellationToken for background camera loops.
- Do not block the UI thread.
- Add comments for non-obvious image processing parameters.

## Detection Requirements

The target feature is a group of thin dark line-like objects.
Each line is counted as one target.

Initial detection pipeline:
1. Convert BGR image to grayscale.
2. Apply Gaussian blur.
3. Apply BlackHat morphology to enhance dark line features.
4. Apply binary threshold.
5. Apply morphology open/close to remove noise.
6. Find contours.
7. Filter contours using:
   - area
   - bounding box width
   - bounding box height
   - height / width ratio
8. Draw one box per valid feature.
9. Show total count.

## Test Requirements

Create a test checklist in docs/test-checklist.md.
Include manual tests for:
- Camera open / close
- Detection on / off
- Correct count on sample images
- No UI freeze during live preview
- No memory growth during 5-minute live preview