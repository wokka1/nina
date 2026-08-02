using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
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

    /// <summary>
    /// Telescope advanced controls (2026-08-02) - Park/Unpark/StopSlew/tracking toggle, all real
    /// ITelescopeVM methods that already existed with no command wired to them. Deliberately
    /// scoped to these well-understood, commonly-used operations rather than a full manual
    /// slew-to-coordinates UI - that's real additional scope (target validation, real physical
    /// mount movement) better suited to its own pass with live hardware verification, matching
    /// this project's established "functional first slice, not full parity" pattern everywhere
    /// else.
    /// </summary>
    [RelayCommand]
    private async Task ParkTelescope() {
        await TelescopeVM.ParkTelescope(new Progress<ApplicationStatus>(), CancellationToken.None);
    }

    [RelayCommand]
    private async Task UnparkTelescope() {
        await TelescopeVM.UnparkTelescope(new Progress<ApplicationStatus>(), CancellationToken.None);
    }

    [RelayCommand]
    private void StopTelescopeSlew() {
        TelescopeVM.StopSlew();
    }

    [RelayCommand]
    private void ToggleTelescopeTracking() {
        var current = TelescopeVM.GetDeviceInfo()?.TrackingEnabled ?? false;
        TelescopeVM.SetTrackingEnabled(!current);
    }

    /// <summary>
    /// Camera cooling controls (2026-08-02) - real ICameraVM.SetCooler/SetTemperature, same
    /// "existing method, no command wired to it" gap as Switch/Telescope above. TargetCoolingTemp
    /// defaults to -10°C, a common real-world OSC/mono cooled-camera setpoint, not a device-
    /// reported default (CameraInfo doesn't expose one) - purely a sane starting value for the
    /// NumericUpDown, the user's own real setpoint is whatever they type before hitting Set.
    /// Uses the plain SetCooler/SetTemperature pair (immediate), not the gradual
    /// CoolCamera/WarmCamera ramp overloads - those take a real IProgress-driven ramp duration
    /// better suited to a dedicated ramp-with-progress-bar control than a quick toggle/set pair,
    /// consistent with keeping this a functional first slice.
    /// </summary>
    [ObservableProperty]
    public partial double TargetCoolingTemp { get; set; } = -10;

    [RelayCommand]
    private void SetCoolerOn() {
        CameraVM.SetCooler(true);
    }

    [RelayCommand]
    private void SetCoolerOff() {
        CameraVM.SetCooler(false);
    }

    [RelayCommand]
    private void SetCameraTemperature() {
        CameraVM.SetTemperature(TargetCoolingTemp);
    }
}
