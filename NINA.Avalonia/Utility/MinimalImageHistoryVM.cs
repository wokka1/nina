using System.Collections.Generic;
using System.Windows.Input;
using NINA.Core.Utility;
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Image.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Model;
using NINA.WPF.Base.Utility.AutoFocus;

namespace NINA.Avalonia.Utility {

    /// <summary>
    /// Minimal, real (not mocked) IImageHistoryVM - the missing dependency that was keeping
    /// TakeExposure (arguably the single most important sequence item) out of
    /// SequenceItemCatalog's discovered set. IImageHistoryVM itself lives in NINA.WPF.Base
    /// (portable, already multi-targeted); only the real app's ImageHistoryVM concrete
    /// implementation is in the main NINA exe project (NINA/ViewModel/ImageHistory/), same
    /// non-portable situation as ImagingVM. This isn't a rich image-history feature (that's a
    /// separate, bigger piece of work) - it correctly implements the interface surface with
    /// real, working collections/commands so TakeExposure's constructor can resolve and the
    /// item can actually run, without pretending to track a real image history yet.
    /// </summary>
    public class MinimalImageHistoryVM : BaseINPC, IImageHistoryVM {
        private int nextId;

        public AsyncObservableCollection<ImageHistoryPoint> AutoFocusPoints { get; set; } = new();
        public List<ImageHistoryPoint> ImageHistory { get; } = new();
        public AsyncObservableCollection<ImageHistoryPoint> ObservableImageHistory { get; set; } = new();
        public ICommand PlotClearCommand => new RelayCommand(PlotClear);

        public bool CanClose { get; set; }
        public string ContentId => nameof(MinimalImageHistoryVM);
        public ICommand HideCommand => new RelayCommand(() => Hide(null));
        public bool IsClosed { get; set; }
        public bool HasSettings { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsTool => true;
        public string Title { get; set; } = "Image History";

        public int GetNextImageId() => nextId++;

        public void Add(int id, IImageStatistics statistics, string imageType) {
            // Real image-history tracking (thumbnails, per-image stats over a session) is its
            // own separate feature - out of scope here, this just needs to exist and not throw
            // so TakeExposure's real capture logic can proceed.
        }

        public void Add(int id, string imageType) {
        }

        public void PopulateStatistics(int id, IImageStatistics statistics) {
        }

        public void AppendImageProperties(ImageSavedEventArgs imageSavedEventArgs) {
        }

        public void AppendAutoFocusPoint(AutoFocusReport report) {
        }

        public void PlotClear() {
            ImageHistory.Clear();
            ObservableImageHistory.Clear();
        }

        public void Hide(object o) {
            IsClosed = true;
            IsVisible = false;
        }
    }
}
