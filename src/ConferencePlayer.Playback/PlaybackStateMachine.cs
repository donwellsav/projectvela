using System;
using System.Threading.Tasks;
using ConferencePlayer.Core;

namespace ConferencePlayer.Playback;

public sealed class PlaybackStateMachine : IDisposable
{
    private readonly IPlaybackEngine _engine;
    private readonly IOutputController _output;
    private readonly IUserPromptService _prompts;
    private readonly AppLogger _logger;
    private readonly AppSettings _settings;

    private bool _savedMuteBeforePanic;
    private bool _wasPlayingBeforePanic;
    private bool _isProcessingError;
    private bool _isPanic;
    private string? _currentPath;

    public PlaybackStateMachine(
        IPlaybackEngine engine,
        IOutputController output,
        IUserPromptService prompts,
        AppLogger logger,
        AppSettings settings)
    {
        _engine = engine;
        _output = output;
        _prompts = prompts;
        _logger = logger;
        _settings = settings;

        _engine.StateChanged += OnEngineStateChanged;
        _engine.EndReached += (s, e) => EndReached?.Invoke(this, EventArgs.Empty);
        _engine.PlaybackError += OnEngineError;
    }

    public LibVLCSharp.Shared.MediaPlayer MediaPlayer => _engine.MediaPlayer;

    public PlaybackState State => _isPanic ? PlaybackState.PanicBlackout : _engine.State;

    public bool IsPanic => _isPanic;

    public bool IsMuted => _engine.IsMuted;

    public float Rate => _engine.Rate;

    public event EventHandler<PlaybackState>? StateChanged;
    public event EventHandler? EndReached;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? SkipRequested;

    public void Load(string filePath, bool autoPlay)
    {
        _currentPath = filePath;
        if (_isPanic) ExitPanic();
        _output.SetBlackout(false); // Ensure visible

        try
        {
            _engine.Load(filePath, autoPlay);
        }
        catch (Exception ex)
        {
            // Treat load validation errors as playback errors
            OnEngineError(this, ex.Message);
        }
    }

    public void Play()
    {
        if (_isPanic) ExitPanic();
        _engine.Play();
    }

    public void Pause()
    {
        // If in panic, we stay in panic but ensure engine is paused.
        if (_isPanic)
        {
             _engine.Pause();
             return;
        }
        _engine.Pause();
    }

    public void Stop()
    {
        if (_isPanic) ExitPanic();
        _output.SetBlackout(true);
        _engine.Stop();
    }

    public void SetRate(float rate) => _engine.SetRate(rate);

    public void SetMute(bool mute) => _engine.SetMute(mute);

    public void NextFrame()
    {
        if (_isPanic) return; // Ignore in panic
        _engine.NextFrame();
    }

    public void Seek(TimeSpan time)
    {
        if (_isPanic) return;
        _engine.Seek(time);
    }

    public void SeekRelative(TimeSpan offset)
    {
        if (_isPanic) return;
        _engine.SeekRelative(offset);
    }

    public void TogglePanic()
    {
        if (_isPanic)
            ExitPanic();
        else
            EnterPanic();
    }

    private void EnterPanic()
    {
        if (_isPanic) return;
        _isPanic = true;
        _logger.Warn("PANIC ON (StateMachine)");

        _savedMuteBeforePanic = _engine.IsMuted;
        _wasPlayingBeforePanic = _engine.State == PlaybackState.Playing;

        // Immediate blackout via controller
        _output.SetBlackout(true);

        // Safety posture
        _engine.Pause();

        if (_settings.PanicMutesAudio)
            _engine.SetMute(true);

        NotifyStateChanged();
    }

    private void ExitPanic()
    {
        if (!_isPanic) return;
        _isPanic = false;
        _logger.Warn("PANIC OFF (StateMachine)");

        // Restore output visibility
        _output.SetBlackout(false);

        // Restore audio
        if (_settings.RestoreAudioAfterPanic)
            _engine.SetMute(_savedMuteBeforePanic);

        // Resume if configured
        if (_settings.ResumePlaybackAfterPanic && _wasPlayingBeforePanic)
            _engine.Play();

        NotifyStateChanged();
    }

    private void OnEngineStateChanged(object? sender, PlaybackState newState)
    {
        if (!_isPanic && !_isProcessingError)
        {
             NotifyStateChanged();
        }
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, State);
    }

    private async void OnEngineError(object? sender, string message)
    {
        if (_isProcessingError) return;
        _isProcessingError = true;

        _logger.Error($"Critical Playback Error: {message}", null!);

        // 1. Safety first: Blackout and Pause
        try
        {
            _output.SetBlackout(true);
            _engine.Pause();
        }
        catch (Exception ex)
        {
             _logger.Error("Failed to force safety state during error handling", ex);
        }

        // Notify listeners we are in error state
        ErrorOccurred?.Invoke(this, message);
        NotifyStateChanged(); // Should reflect Error state if engine is Error

        // 2. Prompt operator
        // Note: _prompts implementation handles UI thread marshalling
        UserChoice choice = UserChoice.Stop;
        try
        {
            choice = await _prompts.ShowPlaybackErrorAsync(message, $"Log: {_logger.LogFilePath}");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to show error prompt", ex);
        }

        _logger.Info($"Operator choice: {choice}");

        _isProcessingError = false;

        switch (choice)
        {
            case UserChoice.Retry:
                if (!string.IsNullOrEmpty(_currentPath))
                {
                    Load(_currentPath!, true);
                }
                break;
            case UserChoice.Skip:
                SkipRequested?.Invoke(this, EventArgs.Empty);
                break;
            case UserChoice.Stop:
            default:
                Stop();
                break;
        }
    }

    public void Dispose()
    {
        _engine.StateChanged -= OnEngineStateChanged;
        _engine.PlaybackError -= OnEngineError;
        // Do not dispose _engine here if it's managed externally (DI scope),
        // but typically PlaybackStateMachine owns the engine usage.
        // If we inject engine, maybe we shouldn't dispose it unless we own it.
        // Assuming we own it or share lifecycle.
        _engine.Dispose();
    }
}
