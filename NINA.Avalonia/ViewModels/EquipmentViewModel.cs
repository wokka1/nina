using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.Avalonia.ViewModels;

/// <summary>
/// Mirrors NINA.ViewModel.EquipmentVM's real shape (a thin aggregator handing each device
/// type's own VM straight through). Camera, Telescope, FilterWheel, Focuser, Rotator, Dome,
/// Guider, and Switch are wired up so far - the other 3 device types (FlatDevice/WeatherData/
/// SafetyMonitor) get added here one at a time as Phase 1 works through them, following the
/// same pattern. The real EquipmentVM takes all 11 as required constructor parameters at once
/// (WPF wires the whole finished app in one shot); this one takes what exists today since
/// Avalonia is being built incrementally.
/// </summary>
public partial class EquipmentViewModel : ViewModelBase {
    public EquipmentViewModel(ICameraVM cameraVM, ITelescopeVM telescopeVM, IFilterWheelVM filterWheelVM, IFocuserVM focuserVM, IRotatorVM rotatorVM, IDomeVM domeVM, IGuiderVM guiderVM, ISwitchVM switchVM) {
        CameraVM = cameraVM;
        TelescopeVM = telescopeVM;
        FilterWheelVM = filterWheelVM;
        FocuserVM = focuserVM;
        RotatorVM = rotatorVM;
        DomeVM = domeVM;
        GuiderVM = guiderVM;
        SwitchVM = switchVM;
    }

    public ICameraVM CameraVM { get; }
    public ITelescopeVM TelescopeVM { get; }
    public IFilterWheelVM FilterWheelVM { get; }
    public IFocuserVM FocuserVM { get; }
    public IRotatorVM RotatorVM { get; }
    public IDomeVM DomeVM { get; }
    public IGuiderVM GuiderVM { get; }
    public ISwitchVM SwitchVM { get; }
}
