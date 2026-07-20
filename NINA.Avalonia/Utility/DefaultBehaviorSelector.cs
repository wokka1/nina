using System;
using NINA.Core.Interfaces;
using NINA.Core.Utility;

namespace NINA.Avalonia.Utility {

    /// <summary>
    /// IPluggableBehaviorSelector&lt;T&gt; that always returns one fixed built-in default and
    /// never has any plugin-supplied alternatives. The real app's PluggableBehaviorSelector
    /// (NINA/Utility/PluggableBehaviorSelector.cs) also enumerates plugin-provided behaviors -
    /// that lives in the main WPF host app project, not referenceable from NINA.Avalonia, and
    /// plugin support is deferred to Phase 5 same as NullEquipmentProviders. This is just enough
    /// to construct NINA.Image's ExposureDataFactory/ImageDataFactory today with the same
    /// built-in defaults (StarDetection/StarAnnotator) a plugin-free real install would use.
    /// </summary>
    public class DefaultBehaviorSelector<T> : IPluggableBehaviorSelector<T> {
        private readonly T defaultBehavior;

        public DefaultBehaviorSelector(T defaultBehavior) {
            this.defaultBehavior = defaultBehavior;
            Behaviors = new AsyncObservableCollection<T> { defaultBehavior };
            selectedBehavior = defaultBehavior;
        }

        public Type GetInterfaceType() => typeof(T);

        public void AddBehavior(object behavior) {
            // No-op: no plugin behaviors to add yet.
        }

        public T GetBehavior() => defaultBehavior;

        public AsyncObservableCollection<T> Behaviors { get; set; }

        private T selectedBehavior;

        public T SelectedBehavior {
            get => selectedBehavior;
            set {
                selectedBehavior = value;
                SelectedBehaviorChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler SelectedBehaviorChanged;
    }
}
