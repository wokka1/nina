using NINA.Image.ImageAnalysis;

namespace NINA.Avalonia.Utility {

    /// <summary>
    /// IStarAnnotator with no real behavior. NINA.Image's real StarAnnotator (which draws star
    /// markers onto a rendered image via Accord.Imaging + System.Drawing.Graphics) is entirely
    /// #if HAS_WPF-gated - a documented, deliberate gap, not an oversight: star *annotation*
    /// drawing needs real Avalonia-native rendering work, same as the rest of the image-display
    /// pipeline, and is explicitly Phase 2 territory per the roadmap. On net10.0, IStarAnnotator
    /// itself has no members beyond IPluggableBehavior's Name/ContentId (its one real method,
    /// GetAnnotatedImage, is also HAS_WPF-only), so there's nothing to stub out yet - this
    /// exists purely so DefaultBehaviorSelector&lt;IStarAnnotator&gt; has an instance to hand out
    /// until Phase 2 provides a real portable annotator.
    /// </summary>
    public class PortableStarAnnotator : IStarAnnotator {
        public string Name => "Portable (no-op)";
        public string ContentId => nameof(PortableStarAnnotator);
    }
}
