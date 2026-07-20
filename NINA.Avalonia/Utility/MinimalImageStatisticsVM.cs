using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Core.Utility;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;

namespace NINA.Avalonia.Utility {

    /// <summary>
    /// Minimal, real (not mocked) IImageStatisticsVM - unlocks constructing a real PluginLoader
    /// for the Plugins tab, same category as MinimalImageHistoryVM/MinimalOptionsVM. The real
    /// concrete ImageStatisticsVM lives in the main NINA exe (NINA/ViewModel/ImageStatisticsVM.cs),
    /// not portable. The real Imaging tab (Phase 2) already computes statistics via
    /// IImageData.Statistics directly - this stub exists purely so plugins that [Import]
    /// IImageStatisticsVM (a dockable panel handle in the real app) get a valid, inert instance.
    /// </summary>
    public class MinimalImageStatisticsVM : BaseINPC, IImageStatisticsVM {
        public AllImageStatistics Statistics { get; set; }

        public Task UpdateStatistics(IImageData imageData) {
            return Task.CompletedTask;
        }

        public bool CanClose { get; set; }
        public string ContentId => nameof(MinimalImageStatisticsVM);
        public ICommand HideCommand => new RelayCommand(() => Hide(null));
        public bool IsClosed { get; set; }
        public bool HasSettings { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsTool => true;
        public string Title { get; set; } = "Image Statistics";

        public void Hide(object o) {
            IsClosed = true;
            IsVisible = false;
        }
    }
}
