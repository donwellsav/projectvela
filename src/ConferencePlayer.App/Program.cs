using System;
using Avalonia;
using Velopack;

namespace ConferencePlayer;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack must run as early as possible so it can handle install/update hooks.
        VelopackApp.Build().Run();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
