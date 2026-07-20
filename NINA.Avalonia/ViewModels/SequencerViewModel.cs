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
/// buttons bound to them.
///
/// AddAnnotation/AddWait are hardcoded stand-ins for the real app's MEF item catalog
/// (SequencerFactory, fed by IPluginLoader's real [Export(typeof(ISequenceItem))] composition -
/// confirmed this lives in the main NINA exe project, not a library, same situation as the
/// imaging ViewModels). Building a genuine equivalent catalog is real, separate design work
/// (System.ComponentModel.Composition itself is a portable NuGet package, so an
/// AssemblyCatalog scan of NINA.Sequencer.dll is plausible - the harder open question is
/// bridging MEF-discovered types with constructor dependencies already wired through this
/// app's own Microsoft.Extensions.DependencyInjection container, e.g. TakeExposure needing
/// camera/filter wheel mediators). Deliberately not attempted in this pass; these two hardcoded
/// item types (both have trivial, dependency-free constructors) just prove the add-to-tree
/// pattern generalizes beyond one type. Per-item-type property editors and real drag-and-drop
/// remain deliberately deferred too.
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

    // Resolves which container a new item should land in: the selected item itself if it's a
    // container, else its parent, else the root. Shared by every AddXxx command below - this is
    // the one bit of real logic standing in for the real app's drag-and-drop-onto-a-container
    // gesture.
    private ISequenceContainer TargetContainer => SelectedItem as ISequenceContainer ?? SelectedItem?.Parent ?? RootContainer;

    [RelayCommand]
    private void AddAnnotation() {
        TargetContainer.Add(new Annotation { Name = "New annotation", Text = "Edit me." });
    }

    [RelayCommand]
    private void AddWait() {
        TargetContainer.Add(new WaitForTimeSpan { Name = "New wait", Time = 60 });
    }
}
