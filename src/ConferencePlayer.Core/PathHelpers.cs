using System;
using System.IO;

namespace ConferencePlayer.Core;

public static class PathHelpers
{
    public static string GetAppDataRoot()
    {
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
