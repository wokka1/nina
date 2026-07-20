using System;
using Microsoft.Extensions.DependencyInjection;
using NINA.Avalonia.Utility;
using NINA.Avalonia.ViewModels;
using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Camera;
using NINA.WPF.Base.ViewModel.Equipment.FilterWheel;
using NINA.WPF.Base.ViewModel.Equipment.Focuser;
using NINA.WPF.Base.ViewModel.Equipment.Telescope;

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
    ///
    /// Phase 1 additions (Camera, the first equipment type): registers the *real* NINA.WPF.Base
    /// CameraVM/CameraChooserVM - not a duplicate/rewritten Avalonia-only class, following the
    /// decision to multi-target NINA.WPF.Base rather than fork its ViewModels. Two dependencies
    /// of the real construction graph (IEquipmentProviders&lt;T&gt; and
    /// IPluggableBehaviorSelector&lt;T&gt;) have real implementations that only exist in the
    /// main WPF host app project (plugin loading, not yet ported) - stood in with the minimal
    /// NullEquipmentProviders/DefaultBehaviorSelector stubs instead of pulling in the whole
    /// plugin subsystem prematurely. Both are explicitly deferred to Phase 5 in the roadmap.
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

            // Mediators - real NINA.WPF.Base.Mediator implementations, unchanged.
            services.AddSingleton<ICameraMediator, CameraMediator>();
            services.AddSingleton<IFilterWheelMediator, FilterWheelMediator>();
            services.AddSingleton<ITelescopeMediator, TelescopeMediator>();
            services.AddSingleton<IApplicationStatusMediator, ApplicationStatusMediator>();
            services.AddSingleton<IDomeMediator, DomeMediator>();
            services.AddSingleton<IFocuserMediator, FocuserMediator>();
            services.AddSingleton<IGuiderMediator, GuiderMediator>();

            services.AddSingleton<ISbigSdk, SbigSdk>();

            // Plugin-shaped dependencies stood in with fixed-default, no-plugin stubs - see
            // class doc comments on both for why. Real plugin loading is Phase 5.
            services.AddSingleton<IPluggableBehaviorSelector<IStarDetection>>(
                f => new DefaultBehaviorSelector<IStarDetection>(new StarDetection()));
            services.AddSingleton<IPluggableBehaviorSelector<IStarAnnotator>>(
                f => new DefaultBehaviorSelector<IStarAnnotator>(new PortableStarAnnotator()));
            services.AddSingleton<IEquipmentProviders<ICamera>, NullEquipmentProviders<ICamera>>();
            services.AddSingleton<IEquipmentProviders<ITelescope>, NullEquipmentProviders<ITelescope>>();
            services.AddSingleton<IEquipmentProviders<IFilterWheel>, NullEquipmentProviders<IFilterWheel>>();
            services.AddSingleton<IEquipmentProviders<IFocuser>, NullEquipmentProviders<IFocuser>>();

            services.AddSingleton<IImageDataFactory, ImageDataFactory>();
            services.AddSingleton<IExposureDataFactory, ExposureDataFactory>();

            // Real NINA.WPF.Base.ViewModel.Equipment.Camera classes - the whole point of
            // multi-targeting NINA.WPF.Base was to reuse these as-is, not rewrite them.
            // CameraVM's last constructor parameter is typed IDeviceChooserVM, not
            // CameraChooserVM - MEDI's automatic constructor injection only matches exactly
            // registered service types, so an explicit factory lambda is needed here (matches
            // the real app's own IoCBindings.cs, which does the same thing for this exact
            // registration rather than relying on automatic resolution).
            services.AddSingleton<CameraChooserVM>();
            services.AddSingleton<ICameraVM, CameraVM>(f =>
                new CameraVM(f.GetRequiredService<IProfileService>(),
                             f.GetRequiredService<ICameraMediator>(),
                             f.GetRequiredService<IFilterWheelMediator>(),
                             f.GetRequiredService<IApplicationStatusMediator>(),
                             f.GetRequiredService<CameraChooserVM>()));

            services.AddSingleton<TelescopeChooserVM>();
            services.AddSingleton<ITelescopeVM, TelescopeVM>(f =>
                new TelescopeVM(f.GetRequiredService<IProfileService>(),
                                f.GetRequiredService<ITelescopeMediator>(),
                                f.GetRequiredService<IApplicationStatusMediator>(),
                                f.GetRequiredService<IDomeMediator>(),
                                f.GetRequiredService<TelescopeChooserVM>()));

            services.AddSingleton<FilterWheelChooserVM>();
            services.AddSingleton<IFilterWheelVM, FilterWheelVM>(f =>
                new FilterWheelVM(f.GetRequiredService<IProfileService>(),
                                  f.GetRequiredService<IFilterWheelMediator>(),
                                  f.GetRequiredService<IFocuserMediator>(),
                                  f.GetRequiredService<IGuiderMediator>(),
                                  f.GetRequiredService<FilterWheelChooserVM>(),
                                  f.GetRequiredService<IApplicationStatusMediator>()));

            services.AddSingleton<FocuserChooserVM>();
            services.AddSingleton<IFocuserVM, FocuserVM>(f =>
                new FocuserVM(f.GetRequiredService<IProfileService>(),
                              f.GetRequiredService<IFocuserMediator>(),
                              f.GetRequiredService<IApplicationStatusMediator>(),
                              f.GetRequiredService<FocuserChooserVM>()));

            services.AddSingleton<EquipmentViewModel>();
            services.AddSingleton<MainViewModel>();

            var provider = services.BuildServiceProvider();
            var mainViewModel = provider.GetRequiredService<MainViewModel>();
            return (provider, mainViewModel);
        }
    }
}
