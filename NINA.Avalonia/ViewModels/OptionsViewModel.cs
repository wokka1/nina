using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Avalonia.Utility;
using NINA.Core.Utility;
using NINA.Core.Utility.ColorSchema;
using NINA.Profile;
using NINA.Profile.Interfaces;

namespace NINA.Avalonia.ViewModels;

/// <summary>
/// Phase 5 first slice: a real settings editor over the active profile's actual settings
/// objects (CameraSettings, TelescopeSettings, etc. - all real, portable NINA.Profile classes,
/// unchanged). Reuses the same generic reflection-based PropertyEditRow built for the
/// Sequencer's item property panel rather than hand-building one view per category (the real
/// WPF app has ~20 dedicated Options sub-views, real design work for each) - same
/// proportion-of-effort trade as the Sequencer editor: uniform simple-type editing today over
/// polished per-category layout.
///
/// Also carries the deferred multi-profile picker from the Phase 0 note (real
/// IProfileService.SelectProfile(), not reimplemented) - deliberately not a modal
/// blocking-the-startup-thread dialog like the real WPF ProfileSelectVM (DispatcherFrame-based,
/// no Avalonia equivalent, and not needed here since ProfileService.TryLoad(null) already
/// handles startup without a picker). Known limitation, not chased: switching profiles here
/// only affects IProfile-backed reads going forward (this tab, anything that reads
/// ActiveProfile fresh) - already-constructed equipment VMs/mediators don't get re-initialized
/// against the new profile's settings, same as the real app would need a restart for in
/// several similar cases.
/// </summary>
public partial class OptionsViewModel : ViewModelBase {
    private readonly IProfileService profileService;

    // NINA.Profile's own scaffolding base classes - properties declared here are
    // framework plumbing (change notification, dirty tracking), not real settings.
    // SerializableINPC itself is marked obsolete upstream (superseded by ObservableObject for
    // new code) but every real settings class still inherits it today, so it's still the
    // correct type to filter against here - not something to "fix" by removing.
#pragma warning disable CS0618
    private static readonly Type[] ScaffoldingBaseTypes = [typeof(Settings), typeof(SerializableINPC)];
#pragma warning restore CS0618
    private static readonly Type[] SupportedPropertyTypes = [typeof(double), typeof(int), typeof(bool), typeof(string)];

    public OptionsViewModel(IProfileService profileService) {
        this.profileService = profileService;

        Categories = new ObservableCollection<string>(
            typeof(IProfile).GetProperties()
                .Where(p => typeof(ISettings).IsAssignableFrom(p.PropertyType))
                .Select(p => p.Name)
                .OrderBy(n => n));

        Profiles = profileService.Profiles;
        SelectedProfile = profileService.Profiles.FirstOrDefault(p => p.Id == profileService.ActiveProfile.Id);

        AvailableThemes = new ObservableCollection<ColorSchema>(
            profileService.ActiveProfile.ColorSchemaSettings.ColorSchemas.Items);
        SelectedTheme = profileService.ActiveProfile.ColorSchemaSettings.ColorSchema;
    }

    /// <summary>
    /// Real NINA.Profile theme support (2026-08-22) - 16 named built-in palettes plus 2 custom
    /// slots (NINA.Core.Utility.ColorSchema.ColorSchemas.ReadColorSchemas), the real data behind
    /// the WPF app's Options -> Application -> Theme picker. SelectedTheme naturally starts on
    /// "Persian Faint" - the real WPF app's own default (ColorSchemaSettings.SetDefaultValues),
    /// nothing special needed since it's read straight off the active profile. UsePlainDefault
    /// defaults to false (real NINA colors on by default) per user decision, superseding this
    /// port's own original default of true, kept as an opt-out for anyone who prefers this
    /// port's plainer look - see wokka1/nina#1. MainWindow.axaml.cs owns actually applying the
    /// change (adds/removes the "nina-themed" class on the root Window) since NinaThemeService
    /// only touches resources, not the View.
    /// </summary>
    public ObservableCollection<ColorSchema> AvailableThemes { get; }

    [ObservableProperty]
    public partial ColorSchema? SelectedTheme { get; set; }

    partial void OnSelectedThemeChanged(ColorSchema? value) {
        if (value == null) {
            return;
        }
        profileService.ActiveProfile.ColorSchemaSettings.ColorSchema = value;
        NinaThemeService.ApplyTheme(value);
    }

    [ObservableProperty]
    public partial bool UsePlainDefault { get; set; } = false;

    public ObservableCollection<string> Categories { get; }

    public AsyncObservableCollection<ProfileMeta> Profiles { get; }

    [ObservableProperty]
    public partial ProfileMeta? SelectedProfile { get; set; }

    [ObservableProperty]
    public partial string ProfileStatusMessage { get; set; } = string.Empty;

    partial void OnSelectedProfileChanged(ProfileMeta? value) {
        if (value == null || value.Id == profileService.ActiveProfile.Id) {
            return;
        }
        if (profileService.SelectProfile(value)) {
            ProfileStatusMessage = $"Switched to profile '{value.Name}'.";
        } else {
            ProfileStatusMessage = $"Could not switch to '{value.Name}' - it may be in use elsewhere.";
        }
    }

    [ObservableProperty]
    public partial string? SelectedCategory { get; set; }

    public ObservableCollection<PropertyEditRow> SettingsProperties { get; } = new();

    partial void OnSelectedCategoryChanged(string? value) {
        SettingsProperties.Clear();
        if (string.IsNullOrEmpty(value)) {
            return;
        }

        var settingsObject = typeof(IProfile).GetProperty(value)?.GetValue(profileService.ActiveProfile);
        if (settingsObject == null) {
            return;
        }

        foreach (var row in BuildPropertyRows(settingsObject)) {
            SettingsProperties.Add(row);
        }
    }

    private static IEnumerable<PropertyEditRow> BuildPropertyRows(object settingsObject) {
        var concreteType = settingsObject.GetType();
        return concreteType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => !ScaffoldingBaseTypes.Contains(p.DeclaringType))
            .Where(p => SupportedPropertyTypes.Contains(Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType))
            .Select(p => new PropertyEditRow(settingsObject, p));
    }
}
