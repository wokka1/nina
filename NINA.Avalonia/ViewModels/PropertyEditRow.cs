using System;
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NINA.Avalonia.ViewModels;

/// <summary>
/// One editable row in the Sequencer's generic property panel (see SequencerViewModel's doc
/// comment for why this is a single reflection-based editor rather than dozens of per-item-type
/// ones - the real app has a dedicated hand-built XAML view per item type, which is real design
/// work for each of the 52+ catalog entries; this trades that polish for immediate, uniform
/// editability across all of them today).
///
/// Deliberately simple: every value is edited as text and converted back via
/// Convert.ChangeType - covers double/int/bool/string cleanly (bool parses "True"/"False" as
/// expected), not more exotic types (enums, nested objects, collections) - those properties are
/// filtered out entirely rather than shown broken, see SequencerViewModel.BuildPropertyRows.
/// </summary>
public partial class PropertyEditRow : ObservableObject {
    private readonly object target;
    private readonly PropertyInfo property;

    public PropertyEditRow(object target, PropertyInfo property) {
        this.target = target;
        this.property = property;
        Value = FormatValue(property.GetValue(target));
    }

    public string Name => property.Name;

    [ObservableProperty]
    public partial string Value { get; set; } = string.Empty;

    partial void OnValueChanged(string value) {
        try {
            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var converted = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            property.SetValue(target, converted);
        } catch {
            // Invalid input for this property's type - leave the underlying value untouched
            // and let the user keep typing rather than throwing mid-edit.
        }
    }

    private static string FormatValue(object? value) {
        return value switch {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }
}
