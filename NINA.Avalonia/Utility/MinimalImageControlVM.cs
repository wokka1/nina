using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Image.Interfaces;
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;

namespace NINA.Avalonia.Utility {

    /// <summary>
    /// Minimal, real (not mocked) IImageControlVM - unlocks constructing a real PluginLoader
    /// for the Plugins tab, same category as MinimalImageHistoryVM/MinimalOptionsVM/
    /// MinimalImageStatisticsVM. The real concrete ImageControlVM (image display, plate-solve
    /// overlay, Bahtinov mask analysis) lives in the main NINA exe
    /// (NINA/ViewModel/ImageControlVM.cs), not portable. Phase 2's real Imaging tab already
    /// covers capture-to-screen directly via ImagingViewModel, without going through this
    /// interface - this stub exists purely so plugins that [Import] IImageControlVM (a
    /// dockable panel handle in the real app) get a valid, inert instance rather than blocking
    /// PluginLoader's construction entirely.
    /// </summary>
    public class MinimalImageControlVM : BaseINPC, IImageControlVM {
        public bool AutoStretch { get; set; }
        public ObservableRectangle BahtinovRectangle { get; set; } = new();
        public ICommand CancelPlateSolveImageCommand => new RelayCommand(() => { });
        public bool DetectStars { get; set; }
        public ICommand DragMoveCommand => new RelayCommand(() => { });
        public double DragResizeBoundary => 0;
        public ICommand DragStartCommand => new RelayCommand(() => { });
        public ICommand DragStopCommand => new RelayCommand(() => { });
        public IAsyncCommand InspectAberrationCommand => new AsyncCommand<bool>(async _ => false);
        public bool IsLiveViewEnabled { get; private set; }
        public IAsyncCommand PlateSolveImageCommand => new AsyncCommand<bool>(async _ => false);
        public AsyncCommand<bool> PrepareImageCommand => new AsyncCommand<bool>(async _ => false);
        public IRenderedImage RenderedImage { get; set; }
        public bool ShowBahtinovAnalyzer { get; set; }
        public bool ShowCrossHair { get; set; }
        public ApplicationStatus Status { get; set; } = new();
        public int ImageRotation { get; set; }

        public void Dispose() {
        }

        public Task<IRenderedImage> PrepareImage(NINA.Image.Interfaces.IImageData data, PrepareImageParameters parameters, CancellationToken cancelToken) {
            return Task.FromResult<IRenderedImage>(null);
        }

        public void UpdateDeviceInfo(CameraInfo cameraInfo) {
        }

        public event EventHandler<ImagePreparedEventArgs> ImagePrepared;

        public bool CanClose { get; set; }
        public string ContentId => nameof(MinimalImageControlVM);
        public ICommand HideCommand => new RelayCommand(() => Hide(null));
        public bool IsClosed { get; set; }
        public bool HasSettings { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsTool => true;
        public string Title { get; set; } = "Image";

        public void Hide(object o) {
            IsClosed = true;
            IsVisible = false;
        }
    }
}
