# NINA.WPF.Base Architecture

## Purpose

`NINA.WPF.Base` contains shared WPF infrastructure used by the main application and plugin-loaded UI components. It is the bridge between lower-level libraries and app-facing view models for common UI/device interactions.

Build shape from `NINA.WPF.Base.csproj`:

- Target frameworks: `net10.0-windows` (WPF enabled, `HAS_WPF` defined) **and** `net10.0` (no WPF - consumed by `NINA.Avalonia`)
- Output type: `Library`

**Multi-targeted since 2026-07-19 as part of the Avalonia port (`feature/avalonia-port`).** This project is no longer WPF-only - `NINA.Avalonia` references its `net10.0` build directly for real, unchanged domain/business logic (mediators, equipment view models, the sky survey subsystem) rather than duplicating it. See "Portability Conventions" below before touching any file in this project - a change that compiles fine for `net10.0-windows` can still break the `net10.0` build silently unless you check both.

## Top-Level Structure

- `Mediator/`
  Dispatcher-style mediator classes such as `ApplicationMediator`, `ApplicationStatusMediator`, `ImagingMediator`, `ImageSaveMediator`, and device mediators for camera, focuser, telescope, guider, dome, switch, and others
- `ViewModel/`
  Shared view-model base types and reusable view models, including:
  - `DockableVM`
  - equipment panels and choosers under `ViewModel/Equipment/*`
  - autofocus support under `ViewModel/AutoFocus/*`
  - `MeridianFlipVM` and `MeridianFlipVMFactory`
  - `PlateSolvingStatusVM`
- `Interfaces/`
  Contracts for mediators, utility services, and shared view models
- `SkySurvey/`
  Survey providers and cache wrappers such as `NASASkySurvey`, `SkyServerSkySurvey`, `ESOSkySurvey`, `Hips2FitsSurvey`, `FileSkySurvey`, `CacheSkySurvey`, `SkySurveyFactory`
- `Resources/`, `View/`, `Behaviors/`, `Utility/`, `Model/`
  Shared XAML resources, controls, behaviors, and support models

## Mediator Pattern

The mediator classes in `Mediator/` are intentionally simple. A typical mediator:

- stores a single registered handler
- exposes a narrow service surface to non-UI code
- forwards calls/events to the handler

Examples:

- `ApplicationStatusMediator`
  Forwards `ApplicationStatus` updates to the single registered `IApplicationStatusVM`.
- `ImagingMediator`
  Forwards capture/prepare/live-view requests to `IImagingVM`.
- `ImageSaveMediator`
  Forwards enqueue and save-event traffic to `IImageSaveController`.

This keeps non-UI code dependent on interfaces instead of concrete view-model implementations.

## Shared View-Model Layer

`ViewModel/BaseVM.cs` and `ViewModel/DockableVM.cs` are the main shared base classes:

- `BaseVM`
  Carries the active `IProfile` through `IProfileService`.
- `DockableVM`
  Adds title, icon, visibility, settings toggles, and docking-related behavior.

The equipment view models under `ViewModel/Equipment/*` are the reusable UI-facing wrappers around the device abstractions from `NINA.Equipment`.

`ViewModel/Equipment/Dome/DomeFollower.cs` is a good example of the project's role: it coordinates dome-following behavior using profile settings and equipment mediators, but it still lives in a reusable UI/support layer rather than the app executable.

## Sky Survey Subsystem

The `SkySurvey/` folder is a contained subsystem for retrieving and caching survey imagery. `SkySurveyFactory` selects implementations based on `SkySurveySource`, and the project provides both remote providers and cache/file-backed variants.

This functionality is shared infrastructure for features like the sky atlas and framing workflows.

**Fully multi-targeted (2026-07-24).** Every file here (except `FileSkySurvey.cs`, still WPF-only - it opens a `Microsoft.Win32.OpenFileDialog`, needs Avalonia's `IStorageProvider` instead, not yet done) has a real `net10.0` rendering/fetch/cache path built on `SixLabors.ImageSharp`/`SixLabors.ImageSharp.Drawing` instead of `System.Drawing`/WPF `BitmapSource`. See "Portability Conventions" below for the pattern used throughout - it is not optional additional context, it is required to modify these files correctly.

- `Portable/` (new folder): `PortableDrawingUtility` (pivot-point shape rotation, rotated-image compositing - ImageSharp.Drawing has no GDI+-style transform stack) and `PortableFonts` (cross-platform font-family fallback chain, since "Segoe UI" doesn't exist outside Windows).
- Every provider/cache/render class has a `*Portable`-suffixed twin of its main method(s) (`GetImage` -> `GetImagePortable`, `Render` -> `RenderPortable`, `Draw(Graphics)` -> `DrawPortable(IImageProcessingContext)`), returning `SkySurveyImagePortable`/raw RGBA32 `byte[]`/`Image<Rgba32>` instead of `SkySurveyImage`/`BitmapSource`.
- `NINA.WPF.Base/Model/FramingAssistant/` (the sky-map annotation model classes: `FrameLineMatrix2`, `FramingDSO`, `FramingConstellation`, `FramingConstellationBoundary`) got the same treatment alongside `SkySurvey/`.

## Dependency Position

Project references:

- `NINA.Core`
- `NINA.Astrometry`
- `NINA.CustomControlLibrary`
- `NINA.Equipment`
- `NINA.Image`
- `NINA.MGEN`
- `NINA.PlateSolving`
- `NINA.Profile`
- `Accord.Imaging (NETStandard)`

The project is referenced by:

- `NINA`
- `NINA.Plugin`
- `NINA.Sequencer`
- `NINA.Test`

That places it between low-level runtime libraries and the final application shell.

## Portability Conventions (net10.0-windows + net10.0 multi-target)

This project's `.csproj` has a `<Compile Remove>` block conditioned on `'$(TargetFramework)'=='net10.0'` that excludes files with no separable portable logic (pure WPF UI: converters, validation rules, XAML code-behind, `IFramingAssistantVM`/`ISkyMapAnnotator` and their WPF-typed contracts). **Before adding a file to that exclusion list, check whether the WPF-only parts can instead be `#if HAS_WPF`-gated within the file** - the sky survey/framing-assistant work above did exactly that for ~14 files that used to be blanket-excluded, so their real portable rendering/fetch logic is reachable from `NINA.Avalonia` today.

Real, hard-won rules for doing this correctly (each one was a real bug found by actually un-excluding a file and rebuilding, or by running a disposable console harness - not by reasoning about it):

- `#if HAS_WPF` gate: any `using System.Windows.Media` / `System.Windows.Media.Imaging` / `System.Windows.Input` is unavailable on `net10.0` here. `System.Windows.Point` resolves on both TFMs (confirmed empirically, reason not fully understood); `System.Windows.Vector` does not - check the actual member, don't assume the whole `System.Windows` namespace behaves uniformly.
- `System.Drawing`/`System.Drawing.Common` types (`Bitmap`, `Graphics`, `Pen`, `SolidBrush`, `Font`) **compile** fine on `net10.0` without any gating - but **do not work at runtime on macOS/Linux** (no `libgdiplus`). Two distinct failure modes to gate for separately:
  1. Any method that actually *uses* `Graphics`/`Bitmap` (draws, encodes, decodes) needs `#if HAS_WPF` even though it would compile without it, or it will throw at the first real call on non-Windows.
  2. **A class's `static` `Pen`/`SolidBrush`/`Font` fields declared as plain field initializers get constructed by the CLR's implicit static constructor the instant *any* member of that class is touched** - including a portable method living in the same file that never itself touches GDI+. Gate the static fields together with their only consumers, or an otherwise-portable method will compile clean and then crash on first call.
- `NINA.Astrometry.Point2d` (not `System.Windows.Point`) and `Coordinates.XYProjectionPortable(...)`/`ViewportFoV.ViewPortCenterPointPortable` (not `XYProjection(...)`/`ViewPortCenterPoint`) are the WPF-free equivalents already established during the Astrometry multi-target pass - use them in any new portable code instead of re-deriving `.X`/`.Y` from the WPF-typed originals.
- A class/interface member that is genuinely a required contract member (e.g. `ISkySurvey.GetImage(...)`) can itself be `#if HAS_WPF`-gated in the interface, so implementers aren't required to provide a WPF-only override on `net10.0` at all.
- **Un-excluding a file and getting `0 errors` is necessary but not sufficient.** Verify with a disposable console harness project (`ProjectReference` to this project, call the real `*Portable` method) that it actually *runs* without throwing - the GDI+ static-field trap above compiles clean and only shows up at runtime.

## Contribution Notes

- Put shared WPF infrastructure here when it is not specific to one top-level app screen.
- Use mediators for cross-layer communication instead of reaching directly into concrete view models from lower layers.
- Keep app-shell composition out of this project; DI registration and final screen wiring still belong to `NINA` (the WPF app) or `NINA.Avalonia` (the cross-platform app).
- When adding shared dockable or equipment-facing UI components, define interfaces here and let the consuming app shell decide how they are composed.
- If you multi-target a *new* file in this project (moving it off the `net10.0` exclusion list), update both the `.csproj` exclusion block's comment and this doc.
