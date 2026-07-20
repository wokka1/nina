using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Utility;

namespace NINA.Avalonia.ViewModels;

/// <summary>
/// Phase 3's first slice: proves the real, now-portable NINA.Sequencer object graph
/// (SequenceRootContainer/SequentialContainer/ISequenceItem, multi-targeted in an earlier
/// commit) can be constructed and displayed in an Avalonia tree, recursively, without any
/// rewrite of the domain model itself.
///
/// Deliberately NOT a real sequence editor yet - the roadmap already flags the Sequencer UI as
/// "a real design problem, not a wire-up" (no 1:1 Avalonia control for WPF's drag-and-drop tree,
/// and the real app's item/condition/trigger "toolbox" is populated via a MEF composition
/// catalog - SequencerFactory - that would need its own real design work to reproduce or
/// replace). This slice hand-builds a small real sequence (a couple of real Annotation items in
/// a nested SequentialContainer) instead of going through that catalog, purely to prove the
/// tree-display mechanics work against the real domain types. Add/remove/reorder UI, the
/// item-type catalog/palette, per-item-type property editors (dozens of item types, each
/// needs its own), and real drag-and-drop are all separate, deliberately deferred follow-ups -
/// see project memory and the tracking issue for the fuller breakdown.
/// </summary>
public class SequencerViewModel : ViewModelBase {
    public SequencerViewModel() {
        RootContainer = new SequenceRootContainer();

        var intro = new Annotation { Name = "Welcome", Text = "This is a real SequenceRootContainer, not a mock." };
        RootContainer.Add(intro);

        var nested = new SequentialContainer { Name = "Example group" };
        nested.Add(new Annotation { Name = "Step 1", Text = "Real ISequenceItem instances, nested for real." });
        nested.Add(new Annotation { Name = "Step 2", Text = "Same recursive Items structure the WPF app's tree uses." });
        RootContainer.Add(nested);
    }

    public SequenceRootContainer RootContainer { get; }
}
