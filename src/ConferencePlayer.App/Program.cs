using System;
using System.IO;
using Avalonia;
using Velopack;
using ConferencePlayer.Core;

namespace ConferencePlayer;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // Only run Velopack if NOT in portable mode.
            if (!PathHelpers.IsPortable)
            {
                // Velopack must run as early as possible so it can handle install/update hooks.
                VelopackApp.Build().Run();
            }

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.log");
            File.WriteAllText(logPath, ex.ToString());
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
