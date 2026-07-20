using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NINA.Avalonia.Utility;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Model;

namespace NINA.Avalonia.ViewModels;

/// <summary>
/// Phase 2's first real slice: drives an actual capture through the real, already-portable
/// pipeline (ICameraVM.Capture/Download -> IExposureData.ToImageData -> IImageData.RenderImage
/// -> IRenderedImage.Stretch -> RawPixels -> a real displayable bitmap via
/// PortableImageBufferConverter) and shows the result. Fresh ViewModel code, not reused from
/// NINA/ViewModel/ImagingVM.cs - that file lives in the main WPF exe project, not a library, so
/// it can't be multi-targeted/referenced the way the equipment VMs were (see project memory for
/// the full reasoning). The underlying pixel pipeline this calls into IS the real, shared,
/// portable code - only this orchestration layer is new.
///
/// Deliberately minimal: one fixed 1-second test exposure via CaptureSequence's own
/// parameter-less-constructor defaults, no exposure time/binning/gain controls yet - proving
/// the pixel-to-screen pipeline works at all is the milestone here, not a full imaging tab.
/// </summary>
public partial class ImagingViewModel : ViewModelBase {
    private readonly ICameraVM cameraVM;
    private CancellationTokenSource captureCts;

    public ImagingViewModel(ICameraVM cameraVM) {
        this.cameraVM = cameraVM;
    }

    [ObservableProperty]
    public partial WriteableBitmap? Image { get; set; }

    [ObservableProperty]
    public partial string CaptureStatus { get; set; } = "No capture yet.";

    [ObservableProperty]
    public partial bool IsCapturing { get; set; }

    [RelayCommand]
    private async Task Capture() {
        if (!cameraVM.GetDeviceInfo().Connected) {
            CaptureStatus = "Camera not connected - connect it on the Equipment tab first.";
            return;
        }

        IsCapturing = true;
        captureCts = new CancellationTokenSource();
        try {
            CaptureStatus = "Exposing...";
            var progress = new Progress<ApplicationStatus>(p => {
                if (!string.IsNullOrEmpty(p.Status)) {
                    Dispatcher.UIThread.Post(() => CaptureStatus = p.Status);
                }
            });

            await cameraVM.Capture(new CaptureSequence(), captureCts.Token, progress);

            CaptureStatus = "Downloading...";
            var exposureData = await cameraVM.Download(captureCts.Token);

            var imageData = await exposureData.ToImageData(progress, captureCts.Token);
            var renderedImage = imageData.RenderImage();

            CaptureStatus = "Stretching...";
            var stretched = await renderedImage.Stretch(0.2, -2.8, false);

            Image = PortableImageBufferConverter.ToWriteableBitmap(stretched.RawPixels);
            CaptureStatus = $"Captured {stretched.RawPixels.Width}x{stretched.RawPixels.Height} ({stretched.RawPixels.Format}) at {DateTime.Now:T}";
        } catch (OperationCanceledException) {
            CaptureStatus = "Capture cancelled.";
        } catch (Exception ex) {
            CaptureStatus = $"Capture failed: {ex.Message}";
        } finally {
            IsCapturing = false;
            captureCts?.Dispose();
            captureCts = null;
        }
    }

    [RelayCommand]
    private void CancelCapture() {
        captureCts?.Cancel();
    }
}
