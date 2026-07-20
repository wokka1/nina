using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Model;
using NINA.Image.Interfaces;

namespace NINA.Avalonia.Utility {

    /// <summary>
    /// Minimal, real (not mocked) IImagingVM - registered as ImagingMediator's handler purely so
    /// ImagingMediator.RegisterHandler() has been called at least once. Without a registered
    /// handler, ImagingMediator's own event accessor ("this.handler.ImagePrepared += value")
    /// NullReferenceExceptions the moment anything subscribes - a real bug this Phase 5 Plugins
    /// tab work surfaced via a disposable console harness (SymbolBroker's constructor subscribes
    /// to ImagePrepared). Not a duplicate of the real capture pipeline - Phase 2's
    /// ImagingViewModel talks to ICameraVM directly and never routes through IImagingMediator,
    /// so this stub's actual capture methods are never called by anything real yet; it only
    /// exists to make ImagingMediator's handler non-null.
    /// </summary>
    public class MinimalImagingVM : IImagingVM {
        private readonly IImageControlVM imageControl;

        public MinimalImagingVM(IImageControlVM imageControl) {
            this.imageControl = imageControl;
        }

        public IImageControlVM ImageControl => imageControl;

        public void DestroyImage() {
        }

        public int GetImageRotation() => 0;

        public void SetImageRotation(int rotation) {
        }

        public void SetSubSambleRectangle(ObservableRectangle observableRectangle) {
        }

        public Task<IExposureData> CaptureImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress, string targetName = "") {
            return Task.FromResult<IExposureData>(null);
        }

        public Task<IRenderedImage> CaptureAndPrepareImage(CaptureSequence sequence, PrepareImageParameters parameters, CancellationToken token, IProgress<ApplicationStatus> progress) {
            return Task.FromResult<IRenderedImage>(null);
        }

        public Task<IRenderedImage> PrepareImage(IImageData data, PrepareImageParameters parameters, CancellationToken token) {
            return Task.FromResult<IRenderedImage>(null);
        }

        public Task<IRenderedImage> PrepareImage(IExposureData data, PrepareImageParameters parameters, CancellationToken token) {
            return Task.FromResult<IRenderedImage>(null);
        }

        public Task<bool> StartLiveView(CaptureSequence sequence, CancellationToken ct) {
            return Task.FromResult(false);
        }

        public event System.EventHandler<ImagePreparedEventArgs> ImagePrepared;

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
        }

        public void UpdateDeviceInfo(FilterWheelInfo deviceInfo) {
        }

        public void UpdateDeviceInfo(FocuserInfo deviceInfo) {
        }

        public void UpdateDeviceInfo(RotatorInfo deviceInfo) {
        }

        public void UpdateDeviceInfo(WeatherDataInfo deviceInfo) {
        }

        public void UpdateEndAutoFocusRun(AutoFocusInfo info) {
        }

        public void UpdateUserFocused(FocuserInfo info) {
        }

        public void Dispose() {
        }
    }
}
