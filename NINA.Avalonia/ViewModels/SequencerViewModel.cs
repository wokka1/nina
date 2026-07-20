using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NINA.Avalonia.Utility;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Trigger;

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
/// ceiling.
///
/// SelectedItemProperties is a generic, reflection-based property panel (PropertyEditRow) -
/// deliberately one uniform editor instead of 52+ hand-built per-item-type views (the real
/// app's actual approach, and real design work for each). Properties are shown unless declared
/// on one of NINA.Sequencer's own known scaffolding base classes
/// (SequenceEntityINPC/SequenceItem/SequenceCondition/SequenceTrigger/SequenceContainer, which
/// carry framework members like Name/Description/Category/Status/HasChanged, not real
/// instruction settings) - broadened from an earlier leaf-type-only filter specifically because
/// some real item types (WaitForAltitude etc.) declare their actual settings on an intermediate
/// abstract base (CoordinatesInstruction) rather than the leaf class, which a leaf-only filter
/// would have missed entirely. Only simple value types (double/int/bool/string) are editable
/// this way; anything else (enums, nested objects, collections) is filtered out rather than
/// shown broken - a real, deliberate limitation, not full parity with the real app's dedicated
/// editors. Real drag-and-drop remains deferred too.
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

    partial void OnSelectedItemChanged(ISequenceEntity? value) {
        SelectedItemProperties.Clear();
        if (value == null) {
            return;
        }
        foreach (var row in BuildPropertyRows(value)) {
            SelectedItemProperties.Add(row);
        }
    }

    public ObservableCollection<PropertyEditRow> SelectedItemProperties { get; } = new();

    private static readonly Type[] SupportedPropertyTypes = [typeof(double), typeof(int), typeof(bool), typeof(string)];

    // NINA.Sequencer's own scaffolding base classes - properties declared on these are framework
    // plumbing (Name/Description/Category/Status/HasChanged/etc.), not real instruction
    // settings. Anything declared below these in the hierarchy (the concrete item type itself,
    // or an intermediate abstract base like CoordinatesInstruction) is fair game.
    private static readonly Type[] ScaffoldingBaseTypes = [
        typeof(SequenceEntityINPC),
        typeof(NINA.Sequencer.SequenceItem.SequenceItem),
        typeof(SequenceCondition),
        typeof(SequenceTrigger),
        typeof(SequenceContainer),
    ];

    private static System.Collections.Generic.IEnumerable<PropertyEditRow> BuildPropertyRows(ISequenceEntity item) {
        var concreteType = item.GetType();
        return concreteType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => !ScaffoldingBaseTypes.Contains(p.DeclaringType))
            .Where(p => SupportedPropertyTypes.Contains(Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType))
            .Select(p => new PropertyEditRow(item, p));
    }

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
