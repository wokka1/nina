using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.Avalonia.ViewModels;

/// <summary>
/// Mirrors NINA.ViewModel.EquipmentVM's real shape (a thin aggregator handing each device
/// type's own VM straight through). Camera, Telescope, and FilterWheel are wired up so far -
/// the other 8 device types (Focuser/Rotator/Dome/Guider/Switch/FlatDevice/WeatherData/
/// SafetyMonitor) get added here one at a time as Phase 1 works through them, following the
/// same pattern. The real EquipmentVM takes all 11 as required constructor parameters at once
/// (WPF wires the whole finished app in one shot); this one takes what exists today since
/// Avalonia is being built incrementally.
/// </summary>
public partial class EquipmentViewModel : ViewModelBase {
    public EquipmentViewModel(ICameraVM cameraVM, ITelescopeVM telescopeVM, IFilterWheelVM filterWheelVM) {
        CameraVM = cameraVM;
        TelescopeVM = telescopeVM;
        FilterWheelVM = filterWheelVM;
    }

    public ICameraVM CameraVM { get; }
    public ITelescopeVM TelescopeVM { get; }
    public IFilterWheelVM FilterWheelVM { get; }
}
