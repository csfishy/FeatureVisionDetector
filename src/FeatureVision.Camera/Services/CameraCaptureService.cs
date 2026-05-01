using OpenCvSharp;

namespace FeatureVision.Camera.Services;

public sealed class CameraCaptureService : IDisposable
{
    public bool IsOpen { get; private set; }

    public void Open(int cameraIndex = 0, VideoCaptureAPIs api = VideoCaptureAPIs.DSHOW)
    {
        IsOpen = true;
    }

    public Mat CaptureFrame()
    {
        throw new NotImplementedException();
    }

    public Task StartAsync(
        Func<Mat, CancellationToken, Task> onFrame,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Close()
    {
        IsOpen = false;
    }

    public void Dispose()
    {
        Close();
    }
}
