using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NINA.Astrometry;

namespace NINA.Avalonia.ViewModels;

/// <summary>
/// Phase 4 first slice: real DSO catalog search, no framing/rendering yet (that needs
/// Avalonia-native sky survey image rendering - a separate, larger problem, see
/// CacheSkySurveyImageFactory in NINA.WPF.Base).
///
/// The real WPF SkyAtlasVM (NINA/ViewModel/SkyAtlasVM.cs, 1290 lines) lives in the main NINA
/// exe project, not a portable library - same fork-in-the-road situation as ImagingVM/
/// SequencerViewModel's catalog. Rather than port all of it, this calls the real portable
/// NINA.Astrometry.DatabaseInteraction directly (unchanged query/filter logic, same SQLite
/// catalog the WPF app reads) and re-implements only the thin UI-facing search/filter glue.
/// </summary>
public partial class SkyAtlasViewModel : ViewModelBase {
    private readonly DatabaseInteraction db = new();

    public SkyAtlasViewModel() {
        _ = LoadFiltersAsync();
    }

    [ObservableProperty]
    public partial ObservableCollection<string> ObjectTypes { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<string> Constellations { get; set; } = new();

    [ObservableProperty]
    public partial string? SelectedObjectType { get; set; }

    [ObservableProperty]
    public partial string? SelectedConstellation { get; set; }

    [ObservableProperty]
    public partial string SearchObjectName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double? MagnitudeFrom { get; set; }

    [ObservableProperty]
    public partial double? MagnitudeThrough { get; set; }

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready.";

    [ObservableProperty]
    public partial ObservableCollection<DeepSkyObject> SearchResults { get; set; } = new();

    [ObservableProperty]
    public partial DeepSkyObject? SelectedResult { get; set; }

    private async Task LoadFiltersAsync() {
        try {
            var types = await db.GetObjectTypes(CancellationToken.None);
            var constellations = await db.GetConstellations(CancellationToken.None);
            ObjectTypes = new ObservableCollection<string>(types);
            Constellations = new ObservableCollection<string>(constellations);
        } catch (Exception ex) {
            StatusMessage = $"Failed to load filter lists: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task Search() {
        IsSearching = true;
        StatusMessage = "Searching...";
        try {
            var searchParams = new DatabaseInteraction.DeepSkyObjectSearchParams {
                Constellation = SelectedConstellation ?? string.Empty,
                DsoTypes = string.IsNullOrEmpty(SelectedObjectType) ? null : new[] { SelectedObjectType },
                ObjectName = SearchObjectName,
                Magnitude = new DatabaseInteraction.DeepSkyObjectSearchFromThru<double?> {
                    From = MagnitudeFrom,
                    Thru = MagnitudeThrough
                },
                Limit = 200
            };

            var results = await db.GetDeepSkyObjects(horizon: null, searchParams: searchParams, token: CancellationToken.None);
            SearchResults = new ObservableCollection<DeepSkyObject>(results);
            StatusMessage = $"{results.Count} object(s) found.";
        } catch (Exception ex) {
            StatusMessage = $"Search failed: {ex.Message}";
        } finally {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void ResetFilters() {
        SelectedObjectType = null;
        SelectedConstellation = null;
        SearchObjectName = string.Empty;
        MagnitudeFrom = null;
        MagnitudeThrough = null;
        SearchResults = new ObservableCollection<DeepSkyObject>();
        StatusMessage = "Ready.";
    }
}
