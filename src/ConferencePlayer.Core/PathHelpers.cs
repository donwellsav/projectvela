using System;
using System.IO;

namespace ConferencePlayer.Core;

public static class PathHelpers
{
    public static bool IsPortable
    {
        get
        {
#if PORTABLE
            return true;
#else
            return File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".portable"));
#endif
        }
    }

    public static string GetAppDataRoot()
    {
        if (IsPortable)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        }

        // Per-user, no-admin location.
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDir, "ConferencePlayer");
    }

    public static string GetDefaultLogsFolder()
    {
        return Path.Combine(GetAppDataRoot(), "Logs");
    }

    public static string GetDefaultSettingsFile()
    {
        return Path.Combine(GetAppDataRoot(), "settings.json");
    }

    public static string GetDefaultPlaylistFile()
    {
        return Path.Combine(GetAppDataRoot(), "playlist.json");
    }
}
