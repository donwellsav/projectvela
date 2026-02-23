using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using ConferencePlayer.Core;

namespace ConferencePlayer.Services;

public class OsExplorerService : IOsExplorerService
{
    public void OpenFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Path cannot be empty.", nameof(folderPath));
        }

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {folderPath}");
        }

        ProcessStartInfo startInfo;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add(folderPath);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            startInfo = new ProcessStartInfo
            {
                FileName = "xdg-open",
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add(folderPath);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            startInfo = new ProcessStartInfo
            {
                FileName = "open",
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add(folderPath);
        }
        else
        {
            throw new PlatformNotSupportedException("OS not supported for file explorer integration.");
        }

        try
        {
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to open folder '{folderPath}'.", ex);
        }
    }
}
