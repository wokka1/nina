using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.Avalonia.ViewModels;

/// <summary>
/// Mirrors NINA.ViewModel.EquipmentVM's real shape (a thin aggregator handing each device
/// type's own VM straight through), but only Camera is wired up so far - the other 10 device
/// types (FilterWheel/Focuser/Rotator/Telescope/Dome/Guider/Switch/FlatDevice/WeatherData/
/// SafetyMonitor) get added here one at a time as Phase 1 works through them, following the
/// Camera pattern this establishes. The real EquipmentVM takes all 11 as required constructor
/// parameters at once (WPF wires the whole finished app in one shot); this one takes what
/// exists today since Avalonia is being built incrementally.
/// </summary>
public partial class EquipmentViewModel : ViewModelBase {
    public EquipmentViewModel(ICameraVM cameraVM) {
        CameraVM = cameraVM;
    }

    public ICameraVM CameraVM { get; }
}
