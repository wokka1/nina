using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using NINA.Core.Model;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.Avalonia.ViewModels;

/// <summary>
/// Mirrors NINA.ViewModel.EquipmentVM's real shape (a thin aggregator handing each device
/// type's own VM straight through). All 11 device types are wired up now - Camera, Telescope,
/// FilterWheel, Focuser, Rotator, Dome, Guider, Switch, FlatDevice, WeatherData, and
/// SafetyMonitor - matching the real EquipmentVM's full constructor shape exactly. Phase 1
/// (equipment connection screens) is complete.
///
/// SetSwitchValueCommand (2026-08-02): Switch's real status is a whole writable/readonly
/// switch-value grid (SwitchInfo.WritableSwitches/ReadonlySwitches, not a few scalar fields the
/// other device types reduce to) - the Switch card originally only proved the chooser/connect/
/// disconnect pattern. This calls the real, unchanged ISwitchVM.SetSwitchValue(switchIndex,
/// value, progress, ct) - already existed, just never had a command wired to it from anywhere
/// portable. Lives here rather than on MainViewModel since EquipmentViewModel already owns
/// SwitchVM directly.
/// </summary>
public partial class EquipmentViewModel : ViewModelBase {
    public EquipmentViewModel(ICameraVM cameraVM, ITelescopeVM telescopeVM, IFilterWheelVM filterWheelVM, IFocuserVM focuserVM, IRotatorVM rotatorVM, IDomeVM domeVM, IGuiderVM guiderVM, ISwitchVM switchVM, IFlatDeviceVM flatDeviceVM, IWeatherDataVM weatherDataVM, ISafetyMonitorVM safetyMonitorVM) {
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
        SafetyMonitorVM = safetyMonitorVM;
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
    public ISafetyMonitorVM SafetyMonitorVM { get; }

    [RelayCommand]
    private async Task SetSwitchValue(IWritableSwitch sw) {
        if (sw == null) {
            return;
        }
        await SwitchVM.SetSwitchValue(sw.Id, sw.TargetValue, new Progress<ApplicationStatus>(), CancellationToken.None);
    }
}
