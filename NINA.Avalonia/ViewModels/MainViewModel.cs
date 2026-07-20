using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Profile.Interfaces;

namespace NINA.Avalonia.ViewModels;

/// <summary>
/// Real navigation shell for NINA.Avalonia, replacing the earlier static placeholder screen.
/// Mirrors NINA.Core.Enum.ApplicationTab (the same 8 top-level sections the WPF app's
/// ApplicationVM/MainWindowVM expose), so the tab set stays in lockstep with the real app
/// rather than inventing a parallel navigation model.
///
/// AvalonDock (WPF's rearrangeable sub-panel docking library) has no Avalonia equivalent -
/// decided (2026-07-19/20, see project memory) to skip panel docking entirely for now and use
/// a plain fixed TabControl for these 8 top-level sections. Revisit only if/when a later phase
/// actually needs rearrangeable sub-panels within a section - most WPF views don't.
/// </summary>
public partial class MainViewModel : ViewModelBase {
    private readonly IProfileService profileService;

    public MainViewModel(IProfileService profileService, EquipmentViewModel equipmentViewModel, ImagingViewModel imagingViewModel, SequencerViewModel sequencerViewModel, SkyAtlasViewModel skyAtlasViewModel, OptionsViewModel optionsViewModel, PluginsViewModel pluginsViewModel) {
        this.profileService = profileService;
        EquipmentVM = equipmentViewModel;
        ImagingVM = imagingViewModel;
        SequencerVM = sequencerViewModel;
        SkyAtlasVM = skyAtlasViewModel;
        OptionsVM = optionsViewModel;
        PluginsVM = pluginsViewModel;

        // Proves NINA.Profile is a real, working dependency now too - shows the profile
        // ProfileService.TryLoad(null) actually selected at startup, not a placeholder string.
        ActiveProfileName = profileService.ActiveProfile?.Name ?? "(no profile loaded)";

        // Refreshes after a real profile switch (OptionsViewModel's profile picker, Phase 5).
        profileService.ProfileChanged += (_, _) => ActiveProfileName = profileService.ActiveProfile?.Name ?? "(no profile loaded)";

        // Updates every second so the running app visibly proves it's live, not a static screenshot.
        clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        clockTimer.Tick += (_, _) => CurrentTime = DateTime.Now.ToString("T");
        clockTimer.Start();
    }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "NINA Avalonia shell is running.";

    [ObservableProperty]
    public partial string CurrentTime { get; set; } = DateTime.Now.ToString("T");

    [ObservableProperty]
    public partial string ActiveProfileName { get; set; } = string.Empty;

    // Proves NINA.Core is a real, working dependency of NINA.Avalonia - pulls a real
    // translated string out of NINA's actual .resx localization resources via the same Loc
    // engine the WPF app uses.
    [ObservableProperty]
    public partial string NinaCoreIntegrationTest { get; set; } = "NINA.Core says: " + Loc.Instance["LblCameraNotConnected"];

    // Int, not the ApplicationTab enum directly - matches NINA.ViewModel.ApplicationVM.TabIndex
    // in the real WPF app (bound straight to TabControl.SelectedIndex there too), kept the same
    // shape here rather than inventing an enum-bound alternative.
    [ObservableProperty]
    public partial int TabIndex { get; set; } = (int)ApplicationTab.EQUIPMENT;

    // Real equipment VM graph (Phase 1) - replaces the earlier static placeholder list.
    public EquipmentViewModel EquipmentVM { get; }

    // Real capture/display pipeline (Phase 2, first slice).
    public ImagingViewModel ImagingVM { get; }

    // Real sequence tree display (Phase 3, first slice).
    public SequencerViewModel SequencerVM { get; }

    // Real DSO catalog search (Phase 4, first slice - search only, no framing/rendering yet).
    public SkyAtlasViewModel SkyAtlasVM { get; }

    // Real settings editor over the active profile (Phase 5, first slice).
    public OptionsViewModel OptionsVM { get; }

    // Real IPluginLoader-backed Plugins tab (Phase 5).
    public PluginsViewModel PluginsVM { get; }

    private readonly DispatcherTimer clockTimer;
}
