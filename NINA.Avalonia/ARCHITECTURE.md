# NINA.Avalonia Architecture

## Purpose

`NINA.Avalonia` is the native macOS/Linux app shell for N.I.N.A., built on Avalonia UI in place of WPF. It is the primary deliverable of the `feature/avalonia-port` branch. It is a new project, not a fork of `NINA` - it composes real, unchanged domain logic from the existing library projects (`NINA.Core`, `NINA.Profile`, `NINA.Astrometry`, `NINA.Equipment`, `NINA.Image`, `NINA.Platesolving`, `NINA.Sequencer`, `NINA.Plugin`, `NINA.WPF.Base`) rather than reimplementing it.

Build shape from `NINA.Avalonia.csproj`:

- Target framework: `net10.0` (no `-windows` suffix - this is the point of the project)
- Output type: `Exe`
- References the `net10.0` build of every backend project above, all of which are multi-targeted (`net10.0-windows;net10.0`) specifically so this project can consume them directly.

## Why This Project Exists (the fork-in-the-road pattern)

Every backend/shared-infrastructure project in the solution was originally WPF-only. Making them multi-target (`net10.0-windows` + `net10.0`) was a multi-week prerequisite effort (see the GitHub issue tracking this branch, `wokka1/nina#1`, for the full history) - each project needed its WPF-only members `#if HAS_WPF`-gated or excluded, one project at a time, verified not to regress the real WPF build.

Some ViewModels the WPF app uses (`ImagingVM`, `SkyAtlasVM`, `SequencerViewModel`'s real MEF-based item catalog UI, etc.) live in the main `NINA` executable project itself, not in a portable library - these are the real "fork in the road": rather than trying to multi-target the entire `NINA` exe (which pulls in the whole WPF app shell), `NINA.Avalonia` writes fresh, Avalonia-native ViewModels that call the same underlying portable domain APIs (`NINA.Astrometry.DatabaseInteraction`, `NINA.WPF.Base.SkySurvey.*`, `NINA.Equipment` device abstractions, etc.) directly. Read the real WPF ViewModel first to understand *what* it does, then write new Avalonia-side glue - don't try to port the WPF-specific parts (data templates, `DependencyProperty`s, `ICommand` boilerplate) verbatim.

## Top-Level Structure

- `CompositionRoot.cs`
  DI composition root. Mirrors the real WPF app's own `NINA/CompositionRoot.cs` + `NINA/Utility/IoCBindings.cs` (both already use `Microsoft.Extensions.DependencyInjection` - this is not a new architectural choice). Builds a `ServiceCollection`, registers real mediators/equipment ViewModels/services, and constructs `MainViewModel`.
- `Program.cs`
  Entry point. Wires `AvaloniaDispatcher : IDispatcher` into `DispatcherProvider.Current` as its first action, mirroring the WPF app's `OnStartup` ordering.
- `ViewModels/`
  One class per top-level tab (`EquipmentViewModel`, `ImagingViewModel`, `SequencerViewModel`, `SkyAtlasViewModel`, `FramingAssistantViewModel`, `OptionsViewModel`, `PluginsViewModel`) plus `MainViewModel` (the navigation shell) and `ViewModelBase`.
- `Views/MainWindow.axaml`
  The entire UI lives in this one file today - one `TabItem` per `NINA.Core.Enum.ApplicationTab` entry. No AvalonDock equivalent is used (decided early in the port: a plain fixed `TabControl`, revisit only if a later phase genuinely needs rearrangeable sub-panels).
- `Utility/`
  Avalonia-specific bridging code: `AvaloniaDispatcher`, `PortableImageBufferConverter` (raw pixel buffers -> `WriteableBitmap`, see below), `NullEquipmentProviders<T>`/`DefaultBehaviorSelector<T>`/`PortableStarAnnotator` (minimal stand-ins for plugin-loading-shaped dependencies that only have real implementations in the main `NINA` exe), `MinimalImageHistoryVM`/`MinimalImagingVM`/`MinimalOptionsVM`/`MinimalImageStatisticsVM`/`MinimalImageControlVM` (real-but-inert stand-ins for interfaces whose only real implementation is main-exe-only).

## The Raw-Pixel-Buffer-to-Bitmap Bridge

A recurring pattern in this project: a backend library computes real pixel data with zero UI-framework dependency (`NINA.Image.IRenderedImage.RawPixels`, `SkyMapAnnotator.SkyMapOverlayRawPixels`, `CacheSkySurveyImageFactory.RenderPortable()`'s return value), and `PortableImageBufferConverter` in this project is the *only* place that turns it into something Avalonia can actually display (`Avalonia.Media.Imaging.WriteableBitmap`). There are two overloads with different input shapes - check which one a new raw-pixel source actually produces before adding a third:

- `ToWriteableBitmap(PortableImageBuffer)` - 16-bit-per-channel sensor data (`Rgb48`/`Gray16`), already stretch-processed. Downshifts each channel (`ushort >> 8`).
- `ToWriteableBitmapFromRgba32(byte[], width, height)` - already 8-bit-per-channel `R,G,B,A` bytes (e.g. `SixLabors.ImageSharp.PixelFormats.Rgba32`'s own in-memory layout via `Image<Rgba32>.CopyPixelDataTo`). No downshift, just a channel swap to Avalonia's `Bgra8888`.

Both overloads exist because the two source pipelines (sensor image processing vs. ImageSharp-based rendering) produce genuinely different pixel layouts - don't try to force one through the other's overload.

## Verification Constraints

**This is an SSH-only, headless Linux dev environment with no display.** Every phase of this project has been verified in layers, and no layer substitutes for the one above it:

1. `dotnet build` (or better, delete `obj`/`bin` first for a genuine cold rebuild) - catches compile errors. **Necessary but not sufficient** - a file that's still excluded from a project's `net10.0` `<Compile Remove>` list will report 0 errors while contributing nothing, and even a file that's included can compile clean while still crashing at runtime (see the GDI+ static-field trap documented in `NINA.WPF.Base/ARCHITECTURE.md`).
2. A disposable console harness (a throwaway `dotnet new console` project, `ProjectReference`-d to whatever you're testing, deleted afterward, never committed) that actually *calls* the new code path with real or realistic inputs. This is how every "verified" claim of substance in this project's history was actually earned, not just asserted.
3. **The user launching the real app on their own Mac and looking at the screen.** No amount of harness testing proves a WriteableBitmap actually renders correctly, that a layout looks right, or that a gesture works - only a real run does. Always ask for this explicitly when a UI change is otherwise "done."

## Contribution Notes

- Before writing a new tab/ViewModel, check whether the real WPF ViewModel's *data layer* already lives in a portable project (`NINA.WPF.Base`, `NINA.Astrometry`, etc.) - if so, call it directly rather than re-deriving its logic.
- Register new services/ViewModels in `CompositionRoot.cs` using the same `services.AddSingleton<T>(...)` style already there; use an explicit factory lambda (not automatic constructor injection) when a constructor parameter's registered type doesn't exactly match its interface, matching the real app's own `IoCBindings.cs` pattern.
- Keep `MainWindow.axaml`'s per-tab XAML reasonably self-contained (a `Grid`/`WrapPanel` per `TabItem`) - there is no separate `View` class per tab today, unlike a typical WPF `View`/`ViewModel` pairing.
- If a new tab needs a main-exe-only WPF ViewModel's real functionality, don't try to multi-target the whole `NINA` exe - write fresh Avalonia-native glue over the same underlying portable APIs (see "Why This Project Exists" above), and note the main-exe file you read for reference in a doc comment.
