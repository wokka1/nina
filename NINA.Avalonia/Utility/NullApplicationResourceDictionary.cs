using NINA.Core.Utility;

namespace NINA.Avalonia.Utility {

    /// <summary>
    /// IApplicationResourceDictionary that never has anything. The real implementation
    /// (NINA.Core.Utility.ApplicationResourceDictionary) reads WPF's Application.Current.Resources
    /// and is entirely WPF-coupled, with no portable equivalent - but some device VMs
    /// (RotatorVM, DomeVM) take IApplicationResourceDictionary as an unconditional constructor
    /// parameter even though their actual use of it (looking up an icon geometry resource) is
    /// #if HAS_WPF-gated internally, so on net10.0 the dependency exists but is never read from.
    /// A no-op stub satisfies the constructor without needing a real resource system yet.
    /// </summary>
    public class NullApplicationResourceDictionary : IApplicationResourceDictionary {
        public object this[string key] => null;
    }
}
