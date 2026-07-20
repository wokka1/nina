using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Utility;

namespace NINA.Avalonia.ViewModels;

/// <summary>
/// Phase 3's first slices: proves the real, now-portable NINA.Sequencer object graph
/// (SequenceRootContainer/SequentialContainer/ISequenceItem, multi-targeted in an earlier
/// commit) can be constructed, displayed, and edited in Avalonia without any rewrite of the
/// domain model itself.
///
/// Deliberately NOT a full sequence editor yet - the roadmap already flags the Sequencer UI as
/// "a real design problem, not a wire-up" (no 1:1 Avalonia control for WPF's drag-and-drop tree,
/// and the real app's item/condition/trigger "toolbox" is populated via a MEF composition
/// catalog - SequencerFactory - that would need its own real design work to reproduce or
/// replace). This hand-builds a small real sequence (a couple of real Annotation items in a
/// nested SequentialContainer) instead of going through that catalog.
///
/// Real finding: IDroppable (which ISequenceEntity/ISequenceItem implement) already exposes
/// MoveUpCommand/MoveDownCommand/DetachCommand and a Parent back-reference - the real app's own
/// reorder/remove logic, already built and portable. No new logic needed for those, just real
/// buttons bound to them. AddAnnotationCommand is new here (the real app would offer every
/// registered item type via the MEF catalog's palette; this adds one hardcoded type to the
/// selected container, or root if the selection isn't a container itself, as a proportionate
/// stand-in for that catalog). Per-item-type property editors (dozens of types) and real
/// drag-and-drop remain deliberately deferred.
/// </summary>
public partial class SequencerViewModel : ViewModelBase {
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

    [ObservableProperty]
    public partial ISequenceEntity? SelectedItem { get; set; }

    [RelayCommand]
    private void AddAnnotation() {
        var target = SelectedItem as ISequenceContainer ?? SelectedItem?.Parent ?? RootContainer;
        target.Add(new Annotation { Name = "New annotation", Text = "Edit me." });
    }
}
