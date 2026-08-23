using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using NINA.Core.Utility.ColorSchema;

namespace NINA.Avalonia.Utility;

/// <summary>
/// Applies real NINA.Profile ColorSchema data (18 built-in themes + 2 custom slots, already
/// portable - see NINA.Core.Utility.ColorSchema.ColorSchema/NINA.Profile.ColorSchemaSettings) to
/// Avalonia resources, under the same semantic brush keys the real WPF app's own
/// NINA.WPF.Base/Resources/StaticResources/Brushes.xaml defines (PrimaryBrush,
/// ButtonBackgroundBrush, etc.) - MainWindow.axaml's ".nina-themed"-scoped styles reference these
/// via DynamicResource, mirroring (at a much smaller scale) the real app's ~40 per-control-type
/// Style files that all key off the same brush names. Doesn't touch Avalonia's own Fluent theme
/// resource keys - a full re-skin of every built-in control's every visual state (hover/pressed/
/// disabled) is real additional scope, deferred; this covers Window/Button/TabItem/ComboBox/
/// TextBox/NumericUpDown/CheckBox base styling, the highest-visible-area win for the effort.
///
/// Plain-default mode (the app's current look, kept as the default - see wokka1/nina#1) isn't
/// "no brushes set", it's the ".nina-themed" class simply not being applied to the root Window
/// (MainWindow.axaml.cs owns that toggle) - the styles below only match under that class, so an
/// un-themed window is pixel-identical to today's stock Fluent look regardless of what's sitting
/// in these resource keys.
/// </summary>
public static class NinaThemeService {

    public static void ApplyTheme(ColorSchema schema) {
        var resources = Application.Current?.Resources;
        if (resources == null) {
            return;
        }

        Set(resources, "PrimaryBrush", schema.PrimaryColorPortable);
        Set(resources, "SecondaryBrush", schema.SecondaryColorPortable);
        Set(resources, "BorderBrush", schema.BorderColorPortable);
        Set(resources, "BackgroundBrush", schema.BackgroundColorPortable);
        Set(resources, "SecondaryBackgroundBrush", schema.SecondaryBackgroundColorPortable);
        Set(resources, "TertiaryBackgroundBrush", schema.TertiaryBackgroundColorPortable);
        Set(resources, "ButtonBackgroundBrush", schema.ButtonBackgroundColorPortable);
        Set(resources, "ButtonBackgroundSelectedBrush", schema.ButtonBackgroundSelectedColorPortable);
        Set(resources, "ButtonForegroundBrush", schema.ButtonForegroundColorPortable);
        Set(resources, "ButtonForegroundDisabledBrush", schema.ButtonForegroundDisabledColorPortable);
        Set(resources, "CrosshairBrush", schema.CrosshairColorPortable);
        Set(resources, "NotificationWarningBrush", schema.NotificationWarningColorPortable);
        Set(resources, "NotificationErrorBrush", schema.NotificationErrorColorPortable);
        Set(resources, "NotificationWarningTextBrush", schema.NotificationWarningTextColorPortable);
        Set(resources, "NotificationErrorTextBrush", schema.NotificationErrorTextColorPortable);
        Set(resources, "SequencerExpressionTextBrush", schema.SequencerExpressionTextColorPortable);
    }

    private static void Set(IResourceDictionary resources, string key, PortableColor c) {
        resources[key] = new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
    }
}
