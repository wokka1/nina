using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Core.Locale;

namespace NINA.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase {
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "NINA Avalonia shell is running.";

    [ObservableProperty]
    public partial string CurrentTime { get; set; } = DateTime.Now.ToString("T");

    // Proves NINA.Core is now a real, working dependency of NINA.Avalonia, not just a
    // compile-time formality - this pulls a real translated string out of NINA's actual
    // .resx localization resources via the same Loc engine the WPF app uses.
    [ObservableProperty]
    public partial string NinaCoreIntegrationTest { get; set; } = "NINA.Core says: " + Loc.Instance["LblCameraNotConnected"];

    public ObservableCollection<EquipmentStatus> Equipment { get; } = new() {
        new EquipmentStatus("Camera", "Not Connected"),
        new EquipmentStatus("Mount", "Not Connected"),
        new EquipmentStatus("Filter Wheel", "Not Connected"),
        new EquipmentStatus("Focuser", "Not Connected"),
        new EquipmentStatus("Guider", "Not Connected"),
    };

    private readonly DispatcherTimer clockTimer;

    public MainViewModel() {
        // Updates every second so the running app visibly proves it's live, not a static screenshot.
        clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        clockTimer.Tick += (_, _) => CurrentTime = DateTime.Now.ToString("T");
        clockTimer.Start();
    }
}

public record EquipmentStatus(string Name, string Status);
