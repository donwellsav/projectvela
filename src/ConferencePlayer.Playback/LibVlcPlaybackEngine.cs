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

    public long Time => MediaPlayer.Time;

    public bool IsMuted => MediaPlayer.Mute;

    public float Rate => MediaPlayer.Rate;

    public event EventHandler? EndReached;
    public event EventHandler<string>? PlaybackError;
    public event EventHandler<PlaybackState>? StateChanged;

    private string? _currentPath;

    public LibVlcPlaybackEngine(LibVLC libVlc, AppLogger logger)
    {
        _logger = logger;
        _libVlc = libVlc;

        MediaPlayer = new MediaPlayer(libVlc);

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

            if (!autoPlay)
            {
                // "Mute & Flag" strategy for seamless preview loading:
                // 1. Force mute (safety against audio blips).
                // 2. Use :start-paused option (primary mechanism).
                // 3. Restore original mute state once strictly paused.
                var originalMute = MediaPlayer.Mute;
                MediaPlayer.Mute = true;
                media.AddOption(":start-paused");

                // Restore mute on first Pause event (or Error)
                void RestoreMuteHandler(object? sender, EventArgs e)
                {
                    MediaPlayer.Paused -= RestoreMuteHandler;
                    MediaPlayer.EncounteredError -= RestoreMuteHandler;
                    try
                    {
                        MediaPlayer.Mute = originalMute;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Failed to restore mute state after load", ex);
                    }
                }

                MediaPlayer.Paused += RestoreMuteHandler;
                MediaPlayer.EncounteredError += RestoreMuteHandler;
                MediaPlayer.Play(media);
            }
            else
            {
                MediaPlayer.Play(media);
            }

            _logger.Info($"Loaded media: {filePath} (autoPlay={autoPlay})");
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

    public void Seek(TimeSpan time)
    {
        try
        {
            if (time < TimeSpan.Zero) time = TimeSpan.Zero;
            var ms = (long)time.TotalMilliseconds;

            // Clamp to duration if known
            var length = MediaPlayer.Length;
            if (length > 0 && ms > length)
                ms = length;

            MediaPlayer.Time = ms;
        }
        catch (Exception ex)
        {
            _logger.Error($"Seek failed: {time}", ex);
        }
    }

    public void SeekRelative(TimeSpan offset)
    {
        try
        {
            var current = MediaPlayer.Time;
            if (current < 0) return; // Not playing or invalid

            var target = current + (long)offset.TotalMilliseconds;
            if (target < 0) target = 0;

            var length = MediaPlayer.Length;
            if (length > 0 && target > length)
                target = length;

            MediaPlayer.Time = target;
        }
        catch (Exception ex)
        {
            _logger.Error($"SeekRelative failed: {offset}", ex);
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
    }
}
