using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.Avalonia.ViewModels;

/// <summary>
/// Mirrors NINA.ViewModel.EquipmentVM's real shape (a thin aggregator handing each device
/// type's own VM straight through). Camera, Telescope, FilterWheel, Focuser, Rotator, Dome,
/// Guider, Switch, FlatDevice, and WeatherData are wired up so far - only SafetyMonitor is
/// left, coming next in Phase 1. The real EquipmentVM takes all 11 as required constructor
/// parameters at once (WPF wires the whole finished app in one shot); this one takes what
/// exists today since Avalonia is being built incrementally.
/// </summary>
public partial class EquipmentViewModel : ViewModelBase {
    public EquipmentViewModel(ICameraVM cameraVM, ITelescopeVM telescopeVM, IFilterWheelVM filterWheelVM, IFocuserVM focuserVM, IRotatorVM rotatorVM, IDomeVM domeVM, IGuiderVM guiderVM, ISwitchVM switchVM, IFlatDeviceVM flatDeviceVM, IWeatherDataVM weatherDataVM) {
        CameraVM = cameraVM;
        TelescopeVM = telescopeVM;
        FilterWheelVM = filterWheelVM;
        FocuserVM = focuserVM;
        RotatorVM = rotatorVM;
        DomeVM = domeVM;
        GuiderVM = guiderVM;
        SwitchVM = switchVM;
        FlatDeviceVM = flatDeviceVM;
        WeatherDataVM = weatherDataVM;
    }

    public ICameraVM CameraVM { get; }
    public ITelescopeVM TelescopeVM { get; }
    public IFilterWheelVM FilterWheelVM { get; }
    public IFocuserVM FocuserVM { get; }
    public IRotatorVM RotatorVM { get; }
    public IDomeVM DomeVM { get; }
    public IGuiderVM GuiderVM { get; }
    public ISwitchVM SwitchVM { get; }
    public IFlatDeviceVM FlatDeviceVM { get; }
    public IWeatherDataVM WeatherDataVM { get; }
}
