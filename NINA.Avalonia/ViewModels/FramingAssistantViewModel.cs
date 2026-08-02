using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NINA.Astrometry;
using NINA.Avalonia.Utility;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.SkySurvey;

namespace NINA.Avalonia.ViewModels;

/// <summary>
/// First real slice of the Framing Assistant tab - renders the grid/DSO/constellation overlay
/// (SkyMapAnnotator.RenderPortable) and the cached-sky-image background
/// (CacheSkySurveyImageFactory.RenderPortable) for a target, bridged to two stacked Avalonia
/// Image controls via the new PortableImageBufferConverter.ToWriteableBitmapFromRgba32
/// overload.
///
/// Fetch-on-demand (2026-08-02): if CacheSkySurveyImageFactory.HasCachedImageForViewport says
/// nothing covers the requested viewport yet, fetches a real image via
/// SkySurveyFactory.Create(source).GetImagePortable(...) and saves it with
/// CacheSkySurvey.SaveImageToCachePortable before rendering - previously this path existed but
/// was never called, so the tab only ever worked if an image happened to already be cached.
///
/// Also fixed a real unit bug found while wiring this up: FieldOfViewArcmin (arcmin, matching
/// the real WPF app's ISkySurvey.GetImage/GetImagePortable convention) was being passed directly
/// into SkyMapAnnotator.Initialize/CacheSkySurveyImageFactory.RenderPortable's vFoVDegrees
/// parameters with no conversion - confirmed via ViewportFoV's constructor (ArcSecWidth =
/// DegreeToArcsec(HFoV)/Width only makes sense if vFoVDegrees really is degrees). At the default
/// value of 60 this rendered a 60-degree field instead of the intended 1 degree (60 arcmin).
///
/// Pan/zoom (2026-08-02): real finding while wiring this up - ChangeFoV (zoom) already had no
/// HAS_WPF gate at all, it only touches ViewportFoV/FrameLineMatrix, not WPF types (the earlier
/// note above was wrong about it). ShiftViewport (pan) really was HAS_WPF-gated, but only because
/// its Vector parameter was a convenience (X,Y) container - the real math underneath
/// (Coordinates.Shift) already took plain doubles, so ViewportFoV.ShiftPortable/
/// SkyMapAnnotator.ShiftViewportPortable are thin overloads, not a real "port." PanCommand nudges
/// the center by 20% of the viewport per press; ZoomCommand scales FieldOfViewArcmin by 0.8/1.25.
/// Both re-render through the same fetch-on-demand path LoadFraming uses, in case panning/zooming
/// moves into a region nothing's cached for yet.
/// </summary>
public partial class FramingAssistantViewModel : ViewModelBase {
    private readonly IProfileService profileService;
    private readonly ITelescopeMediator telescopeMediator;
    private readonly ISkySurveyFactory skySurveyFactory;
    private readonly CacheSkySurvey cache;
    private readonly SkyMapAnnotator annotator;
    private readonly CacheSkySurveyImageFactory imageFactory;

    private const int ViewportWidth = 800;
    private const int ViewportHeight = 800;

    public FramingAssistantViewModel(IProfileService profileService, ITelescopeMediator telescopeMediator, ISkySurveyFactory skySurveyFactory) {
        this.profileService = profileService;
        this.telescopeMediator = telescopeMediator;
        this.skySurveyFactory = skySurveyFactory;

        var cachePath = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "FramingAssistantCache");
        cache = new CacheSkySurvey(cachePath);
        annotator = new SkyMapAnnotator(telescopeMediator, profileService);
        imageFactory = new CacheSkySurveyImageFactory(ViewportWidth, ViewportHeight, cache);
    }

    [ObservableProperty]
    public partial string TargetName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double TargetRADegrees { get; set; }

    [ObservableProperty]
    public partial double TargetDec { get; set; }

    [ObservableProperty]
    public partial double FieldOfViewArcmin { get; set; } = 60;

    [ObservableProperty]
    public partial SkySurveySource Source { get; set; } = SkySurveySource.NASA;

    // Instance property, not static - {Binding FramingAssistantVM.AvailableSources} resolves
    // against the DataContext instance, a static property wouldn't be found the same way.
    public SkySurveySource[] AvailableSources { get; } = new[] {
        SkySurveySource.NASA,
        SkySurveySource.SKYSERVER,
        SkySurveySource.STSCI,
        SkySurveySource.ESO,
        SkySurveySource.HIPS2FITS,
    };

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Enter a target (or pick one on the Sky Atlas tab) and click Load Framing.";

    [ObservableProperty]
    public partial WriteableBitmap? BackgroundImage { get; set; }

    [ObservableProperty]
    public partial WriteableBitmap? OverlayImage { get; set; }

    /// <summary>
    /// Called from Sky Atlas's search results (see MainWindow.axaml) to frame the selected DSO directly,
    /// rather than requiring the user to re-type RA/Dec by hand.
    /// </summary>
    public void SetTarget(DeepSkyObject dso) {
        TargetName = dso.Name;
        TargetRADegrees = dso.Coordinates.RADegrees;
        TargetDec = dso.Coordinates.Dec;
    }

    [RelayCommand]
    private async Task LoadFraming() {
        var coordinates = new Coordinates(TargetRADegrees, TargetDec, Epoch.J2000, Coordinates.RAType.Degrees);
        await RenderFraming(coordinates, FieldOfViewArcmin);
    }

    /// <summary>
    /// Loads a local FITS/TIFF/etc image via FileSkySurvey's real portable decode path
    /// (GetImagePortableFromPath). Called from MainWindow.axaml.cs's file-picker button handler,
    /// not a [RelayCommand] itself - the actual file *picking* has to happen in the View (needs
    /// a real TopLevel/window reference for Avalonia's IStorageProvider), this method only does
    /// the portable work once a path is already chosen.
    /// </summary>
    public async Task LoadFromFileAsync(string filePath) {
        IsLoading = true;
        StatusMessage = $"Loading {Path.GetFileName(filePath)}...";
        try {
            var fileSurvey = (FileSkySurvey)skySurveyFactory.Create(SkySurveySource.FILE);
            var fetched = await fileSurvey.GetImagePortableFromPath(filePath, CancellationToken.None);
            cache.SaveImageToCachePortable(fetched);

            TargetName = fetched.Name;
            TargetRADegrees = fetched.Coordinates.RADegrees;
            TargetDec = fetched.Coordinates.Dec;
            FieldOfViewArcmin = fetched.FoVWidth;

            await RenderFraming(fetched.Coordinates, FieldOfViewArcmin);
        } catch (Exception ex) {
            StatusMessage = $"Loading from file failed: {ex.Message}";
            IsLoading = false;
        }
    }

    /// <summary>
    /// Nudges the viewport center by 20% of the viewport size in the given screen direction and
    /// re-renders. Real pixel deltas (not arcsec) - Coordinates.Shift's pixel-based overload
    /// scales internally via the annotator's own ArcSecWidth/ArcSecHeight, same convention the
    /// original WPF drag handler used.
    /// </summary>
    [RelayCommand]
    private async Task Pan(string direction) {
        if (annotator.ViewportFoV == null) {
            StatusMessage = "Load a framing first before panning.";
            return;
        }
        double deltaX = 0, deltaY = 0;
        var stepX = ViewportWidth * 0.2;
        var stepY = ViewportHeight * 0.2;
        switch (direction) {
            case "Left": deltaX = -stepX; break;
            case "Right": deltaX = stepX; break;
            case "Up": deltaY = -stepY; break;
            case "Down": deltaY = stepY; break;
        }

        var newCenter = annotator.ShiftViewportPortable(deltaX, deltaY);
        TargetRADegrees = newCenter.RADegrees;
        TargetDec = newCenter.Dec;

        await RenderFraming(newCenter, FieldOfViewArcmin);
    }

    [RelayCommand]
    private async Task ZoomIn() {
        FieldOfViewArcmin = Math.Max(1, FieldOfViewArcmin * 0.8);
        var coordinates = new Coordinates(TargetRADegrees, TargetDec, Epoch.J2000, Coordinates.RAType.Degrees);
        await RenderFraming(coordinates, FieldOfViewArcmin);
    }

    [RelayCommand]
    private async Task ZoomOut() {
        FieldOfViewArcmin = Math.Min(600, FieldOfViewArcmin * 1.25);
        var coordinates = new Coordinates(TargetRADegrees, TargetDec, Epoch.J2000, Coordinates.RAType.Degrees);
        await RenderFraming(coordinates, FieldOfViewArcmin);
    }

    private async Task RenderFraming(Coordinates coordinates, double fovArcmin) {
        IsLoading = true;
        StatusMessage = "Rendering...";
        try {
            var fovDegrees = AstroUtil.ArcminToDegree(fovArcmin);

            if (!imageFactory.HasCachedImageForViewport(coordinates, fovDegrees)) {
                StatusMessage = $"Fetching image from {Source}...";
                var progress = new Progress<int>(p => StatusMessage = $"Fetching image from {Source}... {p}%");
                var survey = skySurveyFactory.Create(Source);
                var fetched = await survey.GetImagePortable(TargetName, coordinates, fovArcmin, ViewportWidth, ViewportHeight, CancellationToken.None, progress);
                cache.SaveImageToCachePortable(fetched);
                StatusMessage = "Rendering...";
            }

            await annotator.Initialize(coordinates, fovDegrees, ViewportWidth, ViewportHeight, 0, cache, CancellationToken.None);
            annotator.RenderPortable();
            if (annotator.SkyMapOverlayRawPixels != null) {
                OverlayImage = PortableImageBufferConverter.ToWriteableBitmapFromRgba32(annotator.SkyMapOverlayRawPixels, ViewportWidth, ViewportHeight);
            }

            var backgroundPixels = imageFactory.RenderPortable(coordinates, fovDegrees, 0);
            BackgroundImage = PortableImageBufferConverter.ToWriteableBitmapFromRgba32(backgroundPixels, ViewportWidth, ViewportHeight);

            StatusMessage = $"Framed {TargetName} at RA {TargetRADegrees:0.###} / Dec {TargetDec:0.###}, FoV {fovArcmin:0.#} arcmin.";
        } catch (Exception ex) {
            StatusMessage = $"Framing failed: {ex.Message}";
        } finally {
            IsLoading = false;
        }
    }
}
