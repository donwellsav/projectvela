using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ConferencePlayer.Utils;

public static class FatalErrorHandler
{
    public static void Handle(Exception ex)
    {
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.log");
            File.WriteAllText(logPath, ex.ToString());
        }
        catch
        {
            // Best effort logging: ignore if we can't write to the log file
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                NativeMethods.MessageBox(IntPtr.Zero, "Fatal startup error: " + ex.ToString(), "Project Vela Error", 0x10);
            }
            catch
            {
                // Best effort UI: ignore if we can't show the message box
            }
        }
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);
    }
}
