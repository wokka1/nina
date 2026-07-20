using System.Collections.Generic;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.Avalonia.Utility {

    /// <summary>
    /// IEquipmentProviders&lt;T&gt; that never has any providers. The real app's equivalent,
    /// PluginEquipmentProviders&lt;T&gt;, lives in NINA/Utility/PluginEquipmentProvider.cs - the
    /// main WPF host app project, which was never multi-targeted and isn't referenceable from
    /// NINA.Avalonia. Plugin support is its own later phase (Phase 5 in the roadmap); this stub
    /// just lets CameraChooserVM (and the other device choosers later) construct and run today
    /// with zero third-party/plugin cameras, same as a real profile with no plugins installed.
    /// </summary>
    public class NullEquipmentProviders<T> : IEquipmentProviders<T> where T : IDevice {
        public bool Initialized { get; set; }

        public System.Type GetInterfaceType() => typeof(T);

        public void AddProvider(IEquipmentProvider deviceProvider) {
            // No-op: nothing to add a provider list to yet.
        }

        public Task<IList<IEquipmentProvider<T>>> GetProviders() {
            return Task.FromResult<IList<IEquipmentProvider<T>>>(new List<IEquipmentProvider<T>>());
        }
    }
}
