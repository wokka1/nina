using System;
using Microsoft.Extensions.DependencyInjection;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Avalonia.Utility;
using NINA.Avalonia.ViewModels;
using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyPlanetarium;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Plugin.Messaging;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.Logic;
using NINA.Sequencer.Mediator;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.SkySurvey;
using NINA.WPF.Base.ViewModel;
using NINA.WPF.Base.ViewModel.Equipment.Camera;
using NINA.WPF.Base.ViewModel.Equipment.Dome;
using NINA.WPF.Base.ViewModel.Equipment.FilterWheel;
using NINA.WPF.Base.ViewModel.Equipment.FlatDevice;
using NINA.WPF.Base.ViewModel.Equipment.Focuser;
using NINA.WPF.Base.ViewModel.Equipment.Guider;
using NINA.WPF.Base.ViewModel.Equipment.Rotator;
using NINA.WPF.Base.ViewModel.Equipment.SafetyMonitor;
using NINA.WPF.Base.ViewModel.Equipment.Switch;
using NINA.WPF.Base.ViewModel.Equipment.Telescope;
using NINA.WPF.Base.ViewModel.Equipment.WeatherData;

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
            services.AddSingleton<IRotatorMediator, RotatorMediator>();
            services.AddSingleton<ISafetyMonitorMediator, SafetyMonitorMediator>();
            services.AddSingleton<ISwitchMediator, SwitchMediator>();
            services.AddSingleton<IFlatDeviceMediator, FlatDeviceMediator>();
            services.AddSingleton<IWeatherDataMediator, WeatherDataMediator>();
            services.AddSingleton<IImagingMediator, ImagingMediator>();
            services.AddSingleton<IImageSaveMediator, ImageSaveMediator>();

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
            services.AddSingleton<IEquipmentProviders<IRotator>, NullEquipmentProviders<IRotator>>();
            services.AddSingleton<IEquipmentProviders<IDome>, NullEquipmentProviders<IDome>>();
            services.AddSingleton<IEquipmentProviders<IGuider>, NullEquipmentProviders<IGuider>>();
            services.AddSingleton<IEquipmentProviders<ISwitchHub>, NullEquipmentProviders<ISwitchHub>>();
            services.AddSingleton<IEquipmentProviders<IFlatDevice>, NullEquipmentProviders<IFlatDevice>>();
            services.AddSingleton<IEquipmentProviders<IWeatherData>, NullEquipmentProviders<IWeatherData>>();
            services.AddSingleton<IEquipmentProviders<ISafetyMonitor>, NullEquipmentProviders<ISafetyMonitor>>();
            services.AddSingleton<IApplicationResourceDictionary, NullApplicationResourceDictionary>();
            services.AddSingleton<IDeviceUpdateTimerFactory, DefaultDeviceUpateTimerFactory>();
            services.AddSingleton<IDomeSynchronization, DomeSynchronization>();
            services.AddSingleton<IDomeFollower, DomeFollower>();

            // Real interface (NINA.WPF.Base, portable), minimal-but-real implementation - the
            // concrete ImageHistoryVM lives in the main NINA exe project, not a library, same
            // situation as ImagingVM. See MinimalImageHistoryVM's own doc comment: this exists
            // specifically to unlock TakeExposure and similar items in SequenceItemCatalog, not
            // to provide a real image-history feature yet.
            services.AddSingleton<IImageHistoryVM, MinimalImageHistoryVM>();

            services.AddSingleton<IImageDataFactory, ImageDataFactory>();
            services.AddSingleton<IExposureDataFactory, ExposureDataFactory>();
            services.AddSingleton<ISkySurveyFactory, SkySurveyFactory>();

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

            services.AddSingleton<RotatorChooserVM>();
            services.AddSingleton<IRotatorVM, RotatorVM>(f =>
                new RotatorVM(f.GetRequiredService<IProfileService>(),
                              f.GetRequiredService<IRotatorMediator>(),
                              f.GetRequiredService<RotatorChooserVM>(),
                              f.GetRequiredService<IApplicationResourceDictionary>(),
                              f.GetRequiredService<IApplicationStatusMediator>()));

            services.AddSingleton<DomeChooserVM>();
            services.AddSingleton<IDomeVM, DomeVM>(f =>
                new DomeVM(f.GetRequiredService<IProfileService>(),
                           f.GetRequiredService<IDomeMediator>(),
                           f.GetRequiredService<IApplicationStatusMediator>(),
                           f.GetRequiredService<ITelescopeMediator>(),
                           f.GetRequiredService<DomeChooserVM>(),
                           f.GetRequiredService<IDomeFollower>(),
                           f.GetRequiredService<ISafetyMonitorMediator>(),
                           f.GetRequiredService<IApplicationResourceDictionary>(),
                           f.GetRequiredService<IDeviceUpdateTimerFactory>()));

            services.AddSingleton<GuiderChooserVM>();
            services.AddSingleton<IGuiderVM, GuiderVM>(f =>
                new GuiderVM(f.GetRequiredService<IProfileService>(),
                             f.GetRequiredService<IGuiderMediator>(),
                             f.GetRequiredService<IApplicationStatusMediator>(),
                             f.GetRequiredService<GuiderChooserVM>()));

            services.AddSingleton<SwitchChooserVM>();
            services.AddSingleton<ISwitchVM, SwitchVM>(f =>
                new SwitchVM(f.GetRequiredService<IProfileService>(),
                             f.GetRequiredService<IApplicationStatusMediator>(),
                             f.GetRequiredService<ISwitchMediator>(),
                             f.GetRequiredService<SwitchChooserVM>()));

            services.AddSingleton<FlatDeviceChooserVM>();
            services.AddSingleton<IFlatDeviceVM, FlatDeviceVM>(f =>
                new FlatDeviceVM(f.GetRequiredService<IProfileService>(),
                                 f.GetRequiredService<IFlatDeviceMediator>(),
                                 f.GetRequiredService<IApplicationStatusMediator>(),
                                 f.GetRequiredService<ICameraMediator>(),
                                 f.GetRequiredService<FlatDeviceChooserVM>()));

            services.AddSingleton<WeatherDataChooserVM>();
            services.AddSingleton<IWeatherDataVM, WeatherDataVM>(f =>
                new WeatherDataVM(f.GetRequiredService<IProfileService>(),
                                  f.GetRequiredService<IWeatherDataMediator>(),
                                  f.GetRequiredService<IApplicationStatusMediator>(),
                                  f.GetRequiredService<WeatherDataChooserVM>()));

            services.AddSingleton<SafetyMonitorChooserVM>();
            services.AddSingleton<ISafetyMonitorVM, SafetyMonitorVM>(f =>
                new SafetyMonitorVM(f.GetRequiredService<IProfileService>(),
                                    f.GetRequiredService<ISafetyMonitorMediator>(),
                                    f.GetRequiredService<IApplicationStatusMediator>(),
                                    f.GetRequiredService<SafetyMonitorChooserVM>()));

            // SequenceItemCatalog gets the DI container's own IServiceProvider injected
            // automatically (MEDI registers that for free) - it uses it to resolve each
            // discovered item type's constructor dependencies. See the class doc comment for
            // the full reasoning on why this exists instead of a real MEF CompositionContainer.
            services.AddSingleton<SequenceItemCatalog>();

            // Phase 5 Plugins tab: the ~13 services IPluginLoader's real constructor needs
            // beyond what's already registered above. Most are real, portable, unchanged
            // classes - only IOptionsVM/IImageControlVM/IImageStatisticsVM are the same
            // main-exe-only fork-in-the-road situation as IImageHistoryVM was, so those three
            // get minimal real (not mocked) stand-ins, same pattern as MinimalImageHistoryVM.
            services.AddSingleton<INighttimeCalculator, NighttimeCalculator>();
            services.AddSingleton<ITwilightCalculator, TwilightCalculator>();
            services.AddSingleton<IPlanetariumFactory, PlanetariumFactory>();
            services.AddSingleton<IPlateSolverFactory, PlateSolverFactoryProxy>();
            services.AddSingleton<ISequenceMediator, SequenceMediator>();
            services.AddSingleton<IMessageBroker, MessageBroker>();
            services.AddSingleton<ISymbolBroker, SymbolBroker>();
            services.AddSingleton<ITemplateLinkResolver, TemplateLinkResolver>();
            services.AddSingleton<IApplicationMediator, ApplicationMediator>();
            services.AddSingleton<IAutoFocusVMFactory, BuiltInAutoFocusVMFactory>();
            services.AddSingleton<IMeridianFlipVMFactory, MeridianFlipVMFactory>();
            services.AddSingleton<IOptionsVM, MinimalOptionsVM>();
            services.AddSingleton<IImageStatisticsVM, MinimalImageStatisticsVM>();
            services.AddSingleton<IImageControlVM, MinimalImageControlVM>();
            services.AddSingleton<MinimalImagingVM>();
            services.AddSingleton<IPluginLoader, PluginLoader>();

            services.AddSingleton<EquipmentViewModel>();
            services.AddSingleton<ImagingViewModel>();
            services.AddSingleton<SequencerViewModel>();
            // Phase 4 first slice - constructs its own DatabaseInteraction directly (real,
            // unchanged NINA.Astrometry catalog query code), no equipment/imaging dependencies.
            services.AddSingleton<SkyAtlasViewModel>();
            // Real Framing Assistant tab (grid/DSO/constellation overlay + cached-sky-image background),
            // both rendered via the ImageSharp-based *Portable path added to NINA.WPF.Base's SkySurvey
            // subsystem - see that project's Portable/ folder and SkyMapAnnotator.RenderPortable.
            services.AddSingleton<FramingAssistantViewModel>();
            // Phase 5 first slice - generic settings editor over IProfile's real, portable
            // settings categories, reusing the Sequencer's PropertyEditRow.
            services.AddSingleton<OptionsViewModel>();
            services.AddSingleton<PluginsViewModel>();
            services.AddSingleton<MainViewModel>();

            var provider = services.BuildServiceProvider();

            // ImagingMediator's own ImagePrepared event accessor dereferences its registered
            // handler unconditionally ("this.handler.ImagePrepared += value") - without this,
            // anything that subscribes (e.g. SymbolBroker's constructor, once wired for the
            // Plugins tab) NullReferenceExceptions. A disposable console harness caught this,
            // not a clean compile - see MinimalImagingVM's doc comment for the full story.
            provider.GetRequiredService<IImagingMediator>().RegisterHandler(provider.GetRequiredService<MinimalImagingVM>());

            var mainViewModel = provider.GetRequiredService<MainViewModel>();
            return (provider, mainViewModel);
        }
    }
}
