using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NINA.Plugin.Interfaces;

namespace NINA.Avalonia.ViewModels;

/// <summary>
/// Real Plugins tab, backed by the real, unchanged NINA.Plugin.PluginLoader (now portable -
/// see project memory on the NINA.Plugin multi-target). Loading is user-triggered rather than
/// automatic at startup - Load() does real filesystem scanning of the plugin folder and MEF
/// composition, not something to run unprompted every app launch during development.
///
/// Deliberately resolves IPluginLoader lazily via IServiceProvider inside LoadPlugins(), rather
/// than taking it as a normal constructor dependency. A disposable console harness found that
/// constructing IPluginLoader constructs SymbolBroker (a real, required dependency), whose
/// constructor calls into NINA.Astrometry's SOFA library - the same Windows-only kernel32.dll
/// native dependency documented elsewhere in this project (WaitForAltitude, project memory).
/// Since MainViewModel's constructor graph would otherwise eagerly construct this VM (and
/// therefore IPluginLoader, therefore SymbolBroker) at app startup, that would crash the whole
/// app on macOS/Linux before this tab was ever opened. Deferring construction to the moment the
/// user clicks Load, wrapped in a try/catch, contains the failure to this one tab instead -
/// the underlying SOFA/kernel32 gap itself isn't fixed here, same "documented, not chased"
/// treatment as WaitForAltitude.
/// </summary>
public partial class PluginsViewModel : ViewModelBase {
    private readonly IServiceProvider serviceProvider;

    public PluginsViewModel(IServiceProvider serviceProvider) {
        this.serviceProvider = serviceProvider;
    }

    public ObservableCollection<PluginRow> Plugins { get; } = new();

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Not loaded yet.";

    [RelayCommand]
    private async Task LoadPlugins() {
        StatusMessage = "Loading...";
        try {
            var pluginLoader = serviceProvider.GetRequiredService<IPluginLoader>();
            await pluginLoader.Load();
            Plugins.Clear();
            foreach (var entry in pluginLoader.Plugins) {
                Plugins.Add(new PluginRow(entry.Key.Name, entry.Key.Author, entry.Key.Version?.ToString() ?? "", entry.Value));
            }
            StatusMessage = $"{Plugins.Count} plugin(s) found.";
        } catch (Exception ex) {
            StatusMessage = $"Failed to load: {ex.Message}";
        }
    }
}

public record PluginRow(string Name, string Author, string Version, bool Enabled);
