using System;
using System.Collections.Generic;

namespace ConferencePlayer.Core;

public sealed class AppSettings
{
    // ---- Folder watch ----
    public bool WatchFolderEnabled { get; set; } = false;

    /// <summary>Local folder path to watch. Must be a local path (offline).</summary>
    public string WatchedFolderPath { get; set; } = string.Empty;

    public bool IncludeSubfolders { get; set; } = true;

    public bool FilterEnabled { get; set; } = true;

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

    // ---- Logging ----
    public string LogsFolderPath { get; set; } = string.Empty;

    public void EnsureDefaults()
    {
        if (AllowedExtensions.Count == 0)
        {
            FilterEnabled = false; // if no extensions are defined, disable filter to avoid blocking everything
        }
    }
}
