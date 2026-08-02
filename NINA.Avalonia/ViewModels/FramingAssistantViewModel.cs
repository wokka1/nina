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
/// Not yet built: panning/zooming interaction (ShiftViewport/ChangeFoV are WPF-Vector-typed
/// today, see SkyMapAnnotator.cs's own HAS_WPF gating notes).
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

    public static SkySurveySource[] AvailableSources { get; } = new[] {
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
        IsLoading = true;
        StatusMessage = "Rendering...";
        try {
            var coordinates = new Coordinates(TargetRADegrees, TargetDec, Epoch.J2000, Coordinates.RAType.Degrees);
            var fovDegrees = AstroUtil.ArcminToDegree(FieldOfViewArcmin);

            if (!imageFactory.HasCachedImageForViewport(coordinates, fovDegrees)) {
                StatusMessage = $"Fetching image from {Source}...";
                var progress = new Progress<int>(p => StatusMessage = $"Fetching image from {Source}... {p}%");
                var survey = skySurveyFactory.Create(Source);
                var fetched = await survey.GetImagePortable(TargetName, coordinates, FieldOfViewArcmin, ViewportWidth, ViewportHeight, CancellationToken.None, progress);
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

            StatusMessage = $"Framed {TargetName} at RA {TargetRADegrees:0.###} / Dec {TargetDec:0.###}, FoV {FieldOfViewArcmin} arcmin.";
        } catch (Exception ex) {
            StatusMessage = $"Framing failed: {ex.Message}";
        } finally {
            IsLoading = false;
        }
    }
}
