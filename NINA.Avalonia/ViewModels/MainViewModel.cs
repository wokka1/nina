using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NINA.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase {
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "NINA Avalonia shell is running.";

    [ObservableProperty]
    public partial string CurrentTime { get; set; } = DateTime.Now.ToString("T");

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
