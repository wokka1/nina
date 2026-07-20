using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NINA.Avalonia.Utility;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Utility;

namespace NINA.Avalonia.ViewModels;

/// <summary>
/// Phase 3: proves the real, now-portable NINA.Sequencer object graph
/// (SequenceRootContainer/SequentialContainer/ISequenceItem, multi-targeted in an earlier
/// commit) can be constructed, displayed, and edited in Avalonia without any rewrite of the
/// domain model itself.
///
/// Deliberately NOT a full sequence editor yet - the roadmap already flags the Sequencer UI as
/// "a real design problem, not a wire-up" (no 1:1 Avalonia control for WPF's drag-and-drop tree).
/// This hand-builds a small real sequence (a couple of real Annotation items in a nested
/// SequentialContainer) as the initial tree content.
///
/// Real finding: IDroppable (which ISequenceEntity/ISequenceItem implement) already exposes
/// MoveUpCommand/MoveDownCommand/DetachCommand and a Parent back-reference - the real app's own
/// reorder/remove logic, already built and portable. No new logic needed for those, just real
/// buttons bound to them.
///
/// AddFromCatalogCommand uses the real SequenceItemCatalog (see its own doc comment for the
/// full MEF/DI-bridging design) - a genuine answer to "how does the add-item palette work",
/// not the two-hardcoded-buttons stand-in from the previous pass. Item types whose
/// constructor dependencies aren't yet registered in this app's DI container are silently
/// absent from the catalog rather than shown broken - a real, growing subset, not a permanent
/// ceiling. Per-item-type property editors and real drag-and-drop remain deliberately deferred.
/// </summary>
public partial class SequencerViewModel : ViewModelBase {
    public SequencerViewModel(SequenceItemCatalog catalog) {
        Catalog = catalog;
        RootContainer = new SequenceRootContainer();

        var intro = new Annotation { Name = "Welcome", Text = "This is a real SequenceRootContainer, not a mock." };
        RootContainer.Add(intro);

        var nested = new SequentialContainer { Name = "Example group" };
        nested.Add(new Annotation { Name = "Step 1", Text = "Real ISequenceItem instances, nested for real." });
        nested.Add(new Annotation { Name = "Step 2", Text = "Same recursive Items structure the WPF app's tree uses." });
        RootContainer.Add(nested);
    }

    public SequenceRootContainer RootContainer { get; }

    public SequenceItemCatalog Catalog { get; }

    [ObservableProperty]
    public partial ISequenceEntity? SelectedItem { get; set; }

    [ObservableProperty]
    public partial SequenceItemCatalogEntry? SelectedCatalogEntry { get; set; }

    // Resolves which container a new item should land in: the selected item itself if it's a
    // container, else its parent, else the root. This is the one bit of real logic standing in
    // for the real app's drag-and-drop-onto-a-container gesture.
    private ISequenceContainer TargetContainer => SelectedItem as ISequenceContainer ?? SelectedItem?.Parent ?? RootContainer;

    [RelayCommand]
    private void AddFromCatalog() {
        if (SelectedCatalogEntry == null) {
            return;
        }
        TargetContainer.Add(Catalog.Create(SelectedCatalogEntry));
    }
}
