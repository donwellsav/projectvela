using System;
using System.IO;
using ConferencePlayer.Core;
using LibVLCSharp.Shared;

namespace ConferencePlayer.Playback;

public sealed class LibVlcPlaybackEngine : IPlaybackEngine
{
    private readonly AppLogger _logger;

    private readonly LibVLC _libVlc;
    public MediaPlayer MediaPlayer { get; }

    public PlaybackState State { get; private set; } = PlaybackState.Idle;

    public bool IsMuted => MediaPlayer.Mute;

    public float Rate => MediaPlayer.Rate;

    public event EventHandler? EndReached;
    public event EventHandler<string>? PlaybackError;
    public event EventHandler<PlaybackState>? StateChanged;

    private string? _currentPath;

    public LibVlcPlaybackEngine(AppLogger logger)
    {
        _logger = logger;

        // LibVLCSharp can auto-initialize when using the native LibVLC NuGet packages,
        // but calling Core.Initialize() early can reduce the first-play latency.
        // (We also call it from App startup; calling multiple times is safe.)
        try
        {
            LibVLCSharp.Shared.Core.Initialize();
        }
        catch (Exception ex)
        {
            _logger.Error("Core.Initialize() failed. LibVLC native libs may be missing.", ex);
        }

        // Minimal default options; tune later with real files.
        _libVlc = new LibVLC(
            "--no-video-title-show",
            "--quiet"
        );

        MediaPlayer = new MediaPlayer(_libVlc);

        MediaPlayer.EndReached += (_, __) =>
        {
            _logger.Info("MediaPlayer.EndReached");
            EndReached?.Invoke(this, EventArgs.Empty);
        };

        MediaPlayer.EncounteredError += (_, __) =>
        {
            _logger.Warn("MediaPlayer.EncounteredError");
            SetState(PlaybackState.Error);
            PlaybackError?.Invoke(this, $"Playback error for: {_currentPath ?? "(unknown)"}");
        };

        MediaPlayer.Playing += (_, __) => SetState(PlaybackState.Playing);
        MediaPlayer.Paused += (_, __) => SetState(PlaybackState.Paused);
        MediaPlayer.Stopped += (_, __) => SetState(PlaybackState.Stopped);
    }

    public void Load(string filePath, bool autoPlay)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath is required.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Media file not found.", filePath);

        _currentPath = filePath;

        try
        {
            using var media = new Media(_libVlc, filePath, FromType.FromPath);
            // If autoPlay is false, we still call Play() then immediately Pause()
            // because LibVLC doesn't reliably "load but not play" across formats.
            // This approach is common in LibVLC apps; tune later if needed.
            MediaPlayer.Play(media);

            if (!autoPlay)
                MediaPlayer.Pause();

            _logger.Info($"Loaded media: {filePath}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load/play media: {filePath}", ex);
            SetState(PlaybackState.Error);
            PlaybackError?.Invoke(this, ex.Message);
        }
    }

    public void Play()
    {
        try
        {
            MediaPlayer.SetPause(false);
        }
        catch (Exception ex)
        {
            _logger.Error("Play failed", ex);
            SetState(PlaybackState.Error);
            PlaybackError?.Invoke(this, ex.Message);
        }
    }

    public void Pause()
    {
        try
        {
            MediaPlayer.Pause();
        }
        catch (Exception ex)
        {
            _logger.Error("Pause failed", ex);
            SetState(PlaybackState.Error);
            PlaybackError?.Invoke(this, ex.Message);
        }
    }

    public void Stop()
    {
        try
        {
            MediaPlayer.Stop();
        }
        catch (Exception ex)
        {
            _logger.Error("Stop failed", ex);
            SetState(PlaybackState.Error);
            PlaybackError?.Invoke(this, ex.Message);
        }
    }

    public void SetRate(float rate)
    {
        try
        {
            if (rate <= 0)
                rate = 1.0f;

            MediaPlayer.SetRate(rate);
        }
        catch (Exception ex)
        {
            _logger.Error($"SetRate failed: {rate}", ex);
        }
    }

    public void NextFrame()
    {
        try
        {
            MediaPlayer.NextFrame();
        }
        catch (Exception ex)
        {
            _logger.Error("NextFrame failed", ex);
        }
    }

    public void SetMute(bool mute)
    {
        try
        {
            MediaPlayer.Mute = mute;
        }
        catch (Exception ex)
        {
            _logger.Error($"SetMute failed: {mute}", ex);
        }
    }

    private void SetState(PlaybackState state)
    {
        if (State == state)
            return;

        State = state;
        StateChanged?.Invoke(this, state);
    }

    public void Dispose()
    {
        try
        {
            MediaPlayer.Dispose();
        }
        catch
        {
            // ignore
        }

        try
        {
            _libVlc.Dispose();
        }
        catch
        {
            // ignore
        }
    }
}
