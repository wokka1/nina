using System;
using Microsoft.Extensions.DependencyInjection;
using NINA.Avalonia.Utility;
using NINA.Avalonia.ViewModels;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;

namespace NINA.Avalonia {

    /// <summary>
    /// Composition root for NINA.Avalonia. Mirrors the real WPF app's own
    /// NINA/CompositionRoot.cs + NINA/Utility/IoCBindings.cs, which already use
    /// Microsoft.Extensions.DependencyInjection for the whole ViewModel graph - this is not
    /// a new architectural choice, just the same established pattern applied here.
    ///
    /// The one piece that looks different: WPF bootstraps ProfileService *before* OnStartup
    /// runs by declaring it as a XAML resource in App.xaml (NINA.WPF.Base's ProfileService.xaml
    /// merged dictionary), purely to guarantee it exists before any C# startup code needs it.
    /// Avalonia has no equivalent ordering constraint - Program.Main already controls startup
    /// order directly, so ProfileService is just constructed as a plain object, no XAML trick
    /// needed.
    /// </summary>
    internal static class CompositionRoot {

        public static (IServiceProvider Services, MainViewModel MainViewModel) Compose() {
            var profileService = new ProfileService();

            // TryLoad(null) handles both the "no profiles yet" (creates+selects a default) and
            // "profiles exist" (selects the most-recently-used one) cases entirely on its own -
            // WPF's ProfileSelectView/VM is only shown afterward, and only when the user has
            // multiple profiles and hasn't pinned one via settings/command line. That manual
            // picker UI is real, but not required for a working default load, so it's deferred
            // rather than blocking Phase 0 - see project memory for the explicit decision.
            profileService.TryLoad(null);
            profileService.CreateWatcher();

            var services = new ServiceCollection();
            services.AddSingleton<ProjectVersion>(f => new ProjectVersion(CoreUtil.Version));
            services.AddSingleton<IProfileService>(f => profileService);
            services.AddSingleton<MainViewModel>();

            var provider = services.BuildServiceProvider();
            var mainViewModel = provider.GetRequiredService<MainViewModel>();
            return (provider, mainViewModel);
        }
    }
}
