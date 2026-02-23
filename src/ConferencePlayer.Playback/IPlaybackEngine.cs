using System;
using LibVLCSharp.Shared;

namespace ConferencePlayer.Playback;

public interface IPlaybackEngine : IDisposable
{
    MediaPlayer MediaPlayer { get; }

    PlaybackState State { get; }

    bool IsMuted { get; }
    float Rate { get; }

    event EventHandler? EndReached;
    event EventHandler<string>? PlaybackError;
    event EventHandler<PlaybackState>? StateChanged;

    void Load(string filePath, bool autoPlay);
    void Play();
    void Pause();
    void Stop();

    void SetRate(float rate);

    /// <summary>Advance by one frame (best-effort; codec-dependent).</summary>
    void NextFrame();

    /// <summary>Seeks to a specific time.</summary>
    void Seek(TimeSpan time);

    /// <summary>Seeks relative to the current position.</summary>
    void SeekRelative(TimeSpan offset);

    void SetMute(bool mute);
}
