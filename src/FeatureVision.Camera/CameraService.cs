using OpenCvSharp;

namespace FeatureVision.Camera;

public sealed class CameraService : IDisposable
{
    private readonly object syncRoot = new();
    private VideoCapture? videoCapture;

    public bool IsOpen { get; private set; }

    public void Open(int cameraIndex = 0, VideoCaptureAPIs api = VideoCaptureAPIs.DSHOW)
    {
        Close();

        var capture = new VideoCapture();
        if (!capture.Open(cameraIndex, api))
        {
            capture.Dispose();
            throw new InvalidOperationException($"Unable to open camera index {cameraIndex}.");
        }

        lock (syncRoot)
        {
            videoCapture = capture;
            IsOpen = true;
        }
    }

    public Mat CaptureFrame()
    {
        lock (syncRoot)
        {
            var capture = videoCapture
                ?? throw new InvalidOperationException("Camera is not open.");

            var frame = new Mat();
            if (!capture.Read(frame) || frame.Empty())
            {
                frame.Dispose();
                throw new InvalidOperationException("Unable to capture a frame from the camera.");
            }

            return frame;
        }
    }

    public async Task StartAsync(
        Func<Mat, CancellationToken, Task> onFrame,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onFrame);

        while (!cancellationToken.IsCancellationRequested)
        {
            using var frame = CaptureFrame();
            await onFrame(frame, cancellationToken).ConfigureAwait(false);

            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Close()
    {
        VideoCapture? capture;
        lock (syncRoot)
        {
            capture = videoCapture;
            videoCapture = null;
            IsOpen = false;
        }

        if (capture is null)
        {
            return;
        }

        capture.Release();
        capture.Dispose();
    }

    public void Dispose()
    {
        Close();
    }
}
