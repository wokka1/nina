using Avalonia;
using NINA.Avalonia.Utility;
using NINA.Core.Utility;
using System;

namespace NINA.Avalonia;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Mirrors NINA/App.xaml.cs OnStartup's first line (DispatcherProvider.Current =
        // new WpfDispatcher()) - set before any NINA.Core code that might route through
        // DispatcherProvider.Current runs.
        DispatcherProvider.Current = new AvaloniaDispatcher();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
