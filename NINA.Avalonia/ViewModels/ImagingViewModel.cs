using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NINA.Avalonia.Utility;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Model;

namespace NINA.Avalonia.ViewModels;

/// <summary>
/// Phase 2's real capture-to-screen pipeline: ICameraVM.Capture/Download -> IExposureData.
/// ToImageData -> IImageData.RenderImage -> IRenderedImage.Stretch -> RawPixels -> a real
/// displayable bitmap via PortableImageBufferConverter. Fresh ViewModel code, not reused from
/// NINA/ViewModel/ImagingVM.cs - that file lives in the main WPF exe project, not a library, so
/// it can't be multi-targeted/referenced the way the equipment VMs were (see project memory for
/// the full reasoning). The underlying pixel pipeline this calls into IS the real, shared,
/// portable code - only this orchestration layer is new.
///
/// Exposure time/binning/gain are real, user-settable now - sourced from the connected camera's
/// own CameraInfo.BinningModes/Gains via GetDeviceInfo(), refreshed whenever the camera connects
/// (IDeviceVM's Connected event) since those lists are camera-specific and only known once a
/// real device is attached.
///
/// Star detection runs after every capture via the real, already-portable
/// StarDetection.DetectPortable() (called internally by IRenderedImage.DetectStars() on
/// net10.0 - see RenderedImage.cs's own #if HAS_WPF/#else split) - shown as text stats
/// (HFR/star count/eccentricity), not a visual marker overlay. Real visual markers need
/// per-star pixel-to-display-coordinate math against the Image control's Stretch="Uniform"
/// scaling/letterboxing - genuine additional UI work, deliberately deferred rather than rushed;
/// the numeric feedback alone is a real, working consumer of the portable detection pipeline
/// and arguably the more actionable one for judging focus/image quality anyway. annotateImage
/// is passed false to DetectStars() since the portable side has no annotator to draw with
/// regardless (PortableStarAnnotator is a real no-op, see its own doc comment) - passing true
/// would just be requesting work that silently can't happen.
/// </summary>
public partial class ImagingViewModel : ViewModelBase {
    private readonly ICameraVM cameraVM;
    private CancellationTokenSource captureCts;

    public ImagingViewModel(ICameraVM cameraVM) {
        this.cameraVM = cameraVM;
        cameraVM.Connected += OnCameraConnected;
        RefreshCameraSettings();
    }

    private Task OnCameraConnected(object sender, EventArgs e) {
        Dispatcher.UIThread.Post(RefreshCameraSettings);
        return Task.CompletedTask;
    }

    private void RefreshCameraSettings() {
        var info = cameraVM.GetDeviceInfo();
        if (info == null) {
            return;
        }

        AvailableBinningModes.Clear();
        if (info.BinningModes != null) {
            foreach (var mode in info.BinningModes) {
                AvailableBinningModes.Add(mode);
            }
        }
        SelectedBinning ??= AvailableBinningModes.Count > 0 ? AvailableBinningModes[0] : new BinningMode(1, 1);

        GainMin = info.GainMin;
        GainMax = info.GainMax;
        if (Gain < GainMin || Gain > GainMax) {
            Gain = info.DefaultGain >= 0 ? info.DefaultGain : GainMin;
        }
    }

    [ObservableProperty]
    public partial WriteableBitmap? Image { get; set; }

    [ObservableProperty]
    public partial string CaptureStatus { get; set; } = "No capture yet.";

    [ObservableProperty]
    public partial bool IsCapturing { get; set; }

    [ObservableProperty]
    public partial double ExposureTime { get; set; } = 1.0;

    public ObservableCollection<BinningMode> AvailableBinningModes { get; } = new();

    [ObservableProperty]
    public partial BinningMode? SelectedBinning { get; set; }

    [ObservableProperty]
    public partial int Gain { get; set; } = -1;

    [ObservableProperty]
    public partial int GainMin { get; set; } = -1;

    [ObservableProperty]
    public partial int GainMax { get; set; } = -1;

    [ObservableProperty]
    public partial string StarDetectionStatus { get; set; } = string.Empty;

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

            var sequence = new CaptureSequence {
                ExposureTime = ExposureTime,
                Binning = SelectedBinning ?? new BinningMode(1, 1),
                Gain = Gain,
            };

            await cameraVM.Capture(sequence, captureCts.Token, progress);

            CaptureStatus = "Downloading...";
            var exposureData = await cameraVM.Download(captureCts.Token);

            var imageData = await exposureData.ToImageData(progress, captureCts.Token);
            var renderedImage = imageData.RenderImage();

            CaptureStatus = "Stretching...";
            var stretched = await renderedImage.Stretch(0.2, -2.8, false);

            Image = PortableImageBufferConverter.ToWriteableBitmap(stretched.RawPixels);
            CaptureStatus = $"Captured {stretched.RawPixels.Width}x{stretched.RawPixels.Height} ({stretched.RawPixels.Format}) at {DateTime.Now:T}";

            CaptureStatus = "Detecting stars...";
            var analyzed = await stretched.DetectStars(
                annotateImage: false,
                StarSensitivityEnum.Normal,
                NoiseReductionEnum.None,
                captureCts.Token,
                progress);
            var analysis = analyzed.RawImageData.StarDetectionAnalysis;
            StarDetectionStatus = analysis.DetectedStars > 0
                ? $"{analysis.DetectedStars} stars detected, avg HFR {analysis.HFR:0.00}px, eccentricity {analysis.Eccentricity:0.00}"
                : "No stars detected.";
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
