using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConferencePlayer.Core;

public sealed class AppSettings
{
    // ---- Folder watch ----
    public bool WatchFolderEnabled { get; set; } = false;

    /// <summary>Local folder path to watch. Must be a local path (offline).</summary>
    public string WatchedFolderPath { get; set; } = string.Empty;

    public bool IncludeSubfolders { get; set; } = true;

    // NOTE (v1): watched-folder filtering UI is deferred.
    // We keep the underlying fields for a future release.
    /// <summary>
    /// Case-insensitive extensions, including the leading dot (e.g. ".mp4").
    /// </summary>
    public List<string> AllowedExtensions { get; set; } = new()
    {
        // Video (common)
        ".mp4", ".mov", ".mkv", ".avi", ".wmv", ".m4v", ".mpg", ".mpeg", ".ts", ".m2ts", ".mts", ".webm", ".flv", ".ogv",
        // Pro / production
        ".mxf",
        // Audio (common)
        ".wav", ".mp3", ".aac", ".m4a", ".flac", ".wma", ".aiff", ".aif", ".opus", ".ogg"
    };

    // ---- Playback ----
    public bool AutoAdvancePlaylist { get; set; } = true;


    /// <summary>
    /// If enabled, when reaching the end of the playlist (during auto-advance), loop back to the first item.
    /// </summary>
    public bool LoopPlaylist { get; set; } = false;

    /// <summary>
    /// Persist the playlist to disk (per-user) so it can be restored on next launch.
    /// </summary>
    public bool PersistPlaylist { get; set; } = true;

    /// <summary>
    /// Enable the operator Preview window that preloads the next item (paused on first frame).
    /// </summary>
    public bool EnablePreviewWindow { get; set; } = true;

    /// <summary>
    /// Preview cue mode.
    /// Default: cue the next playlist item.
    /// Option: cue the currently selected item.
    /// </summary>
    public bool PreviewCuesSelectedItem { get; set; } = false;

    /// <summary>
    /// Preview audio monitoring. Default is OFF (preview is muted) to avoid accidental audio bleed.
    /// </summary>
    public bool PreviewAudioEnabled { get; set; } = false;

    // ---- Panic blackout ----
    public bool PanicMutesAudio { get; set; } = true;

    /// <summary>When leaving panic mode, restore the previous mute state.</summary>
    public bool RestoreAudioAfterPanic { get; set; } = true;


    /// <summary>
    /// If enabled, leaving panic will automatically resume playback if it was playing before panic.
    /// Default is OFF (operator must press Play).
    /// </summary>
    public bool ResumePlaybackAfterPanic { get; set; } = false;

    // ---- Output display ----
    /// <summary>
    /// A best-effort identifier for the preferred output screen. We store the Avalonia Screen.DisplayName if available.
    /// </summary>
    public string PreferredOutputScreenName { get; set; } = string.Empty;

    // ---- Hotkeys (Defaults = Option 1) ----
    // Stored as string representations of KeyGesture (e.g. "Ctrl+O", "Space", "F12")

    public string HotKey_PlayPause { get; set; } = "Space";
    public string HotKey_Stop { get; set; } = "S";

    // Transport (Immediate Playback)
    public string HotKey_PlayNext { get; set; } = "PageDown";
    public string HotKey_PlayPrev { get; set; } = "PageUp";

    // Frame Step
    public string HotKey_FrameStep { get; set; } = "F";

    // Cueing / Selection (No Playback)
    public string HotKey_SelectNext { get; set; } = "Ctrl+Right";
    public string HotKey_SelectPrev { get; set; } = "Ctrl+Left";

    // Seeking
    public string HotKey_SeekForward { get; set; } = "Shift+Right";
    public string HotKey_SeekBack { get; set; } = "Shift+Left";

    // Speed
    public string HotKey_IncreaseSpeed { get; set; } = "OemCloseBrackets"; // "]"
    public string HotKey_DecreaseSpeed { get; set; } = "OemOpenBrackets";  // "["

    // Safety
    public string HotKey_Panic { get; set; } = "F12";

    // File Operations
    public string HotKey_AddFiles { get; set; } = "Ctrl+O";
    public string HotKey_AddFolder { get; set; } = "Ctrl+Shift+O";

    // ---- Logging ----
    public string LogsFolderPath { get; set; } = string.Empty;

    public void EnsureDefaults()
    {
        // Defaults logic if needed.
    }

    private static readonly string[] DangerousExtensions = new[]
    {
        ".exe", ".dll", ".bat", ".cmd", ".ps1", ".vbs", ".js", ".jar", ".msi", ".com", ".scr"
    };

    /// <summary>
    /// Validates and sanitizes settings to prevent security issues.
    /// removes dangerous extensions and resets invalid paths.
    /// </summary>
    public void Sanitize()
    {
        // Remove dangerous extensions
        if (AllowedExtensions != null)
        {
            AllowedExtensions.RemoveAll(ext =>
                DangerousExtensions.Any(danger =>
                    string.Equals(ext, danger, StringComparison.OrdinalIgnoreCase)));
        }

        // Validate LogsFolderPath
        if (string.IsNullOrWhiteSpace(LogsFolderPath) || LogsFolderPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            LogsFolderPath = PathHelpers.GetDefaultLogsFolder();
        }

        // Validate WatchedFolderPath
        if (!string.IsNullOrWhiteSpace(WatchedFolderPath) && WatchedFolderPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            WatchedFolderPath = string.Empty;
        }
    }
}
