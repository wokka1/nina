using NINA.Core.Model;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.Avalonia.Utility {

    /// <summary>
    /// Minimal, real (not mocked) IOptionsVM - unlocks constructing a real PluginLoader for
    /// the Plugins tab. The real concrete OptionsVM lives in the main NINA exe
    /// (NINA/ViewModel/OptionsVM.cs), not portable - same fork-in-the-road situation as
    /// ImagingVM/MinimalImageHistoryVM before it. The interface itself is tiny (image file
    /// naming pattern registration) and only exists so plugins that [Import] IOptionsVM can add
    /// their own filename patterns to Options' pattern picker - a real Options screen with that
    /// picker is a separate, bigger feature than this Phase 5 slice's generic settings editor.
    /// </summary>
    public class MinimalOptionsVM : IOptionsVM {
        public void AddImagePattern(ImagePattern pattern) {
        }

        public void RemoveImagePattern(string key) {
        }
    }
}
