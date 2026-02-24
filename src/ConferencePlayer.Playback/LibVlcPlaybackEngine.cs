using System;
using System.IO;
using System.Threading.Tasks;
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

    public async Task LoadAsync(string filePath, bool autoPlay)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath is required.", nameof(filePath));

        _currentPath = filePath;

        // Perform I/O-heavy initialization in a background task
        await Task.Run(() =>
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Media file not found.", filePath);

            TryAction(() =>
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
            }, $"Failed to load/play media: {filePath}", isCritical: true);
        });
    }

    public void Play()
    {
        TryAction(() => MediaPlayer.SetPause(false), "Play failed", isCritical: true);
    }

    public void Pause()
    {
        TryAction(() => MediaPlayer.Pause(), "Pause failed", isCritical: true);
    }

    public void Stop()
    {
        TryAction(() => MediaPlayer.Stop(), "Stop failed", isCritical: true);
    }

    public void SetRate(float rate)
    {
        TryAction(() =>
        {
            if (rate <= 0)
                rate = 1.0f;

            MediaPlayer.SetRate(rate);
        }, $"SetRate failed: {rate}");
    }

    public void NextFrame()
    {
        TryAction(() => MediaPlayer.NextFrame(), "NextFrame failed");
    }

    public void Seek(TimeSpan time)
    {
        TryAction(() =>
        {
            if (time < TimeSpan.Zero) time = TimeSpan.Zero;
            var ms = (long)time.TotalMilliseconds;

            // Clamp to duration if known
            var length = MediaPlayer.Length;
            if (length > 0 && ms > length)
                ms = length;

            MediaPlayer.Time = ms;
        }, $"Seek failed: {time}");
    }

    public void SeekRelative(TimeSpan offset)
    {
        TryAction(() =>
        {
            var current = MediaPlayer.Time;
            if (current < 0) return; // Not playing or invalid

            var target = current + (long)offset.TotalMilliseconds;
            if (target < 0) target = 0;

            var length = MediaPlayer.Length;
            if (length > 0 && target > length)
                target = length;

            MediaPlayer.Time = target;
        }, $"SeekRelative failed: {offset}");
    }

    public void SetMute(bool mute)
    {
        TryAction(() => MediaPlayer.Mute = mute, $"SetMute failed: {mute}");
    }

    private void SetState(PlaybackState state)
    {
        if (State == state)
            return;

        State = state;
        StateChanged?.Invoke(this, state);
    }

    private void TryAction(Action action, string errorMessage, bool isCritical = false)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _logger.Error(errorMessage, ex);
            if (isCritical)
            {
                SetState(PlaybackState.Error);
                PlaybackError?.Invoke(this, ex.Message);
            }
        }
    }

    public void Dispose()
    {
        try
        {
            MediaPlayer.Dispose();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to dispose MediaPlayer", ex);
        }
    }
}
