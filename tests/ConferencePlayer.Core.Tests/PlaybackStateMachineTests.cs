using System;
using System.Threading.Tasks;
using Xunit;
using ConferencePlayer.Core;
using ConferencePlayer.Playback;
using LibVLCSharp.Shared;

namespace ConferencePlayer.Core.Tests;

public class PlaybackStateMachineTests
{
    private class MockEngine : IPlaybackEngine
    {
        public MediaPlayer MediaPlayer => throw new NotImplementedException();

        public long Time => 0;

        public PlaybackState State { get; private set; } = PlaybackState.Idle;

        public bool IsMuted { get; private set; }
        public float Rate { get; private set; } = 1.0f;

#pragma warning disable CS0067
        public event EventHandler? EndReached;
#pragma warning restore CS0067
        public event EventHandler<string>? PlaybackError;
        public event EventHandler<PlaybackState>? StateChanged;

        public TimeSpan LastSeekTime { get; private set; }
        public TimeSpan LastSeekRelativeOffset { get; private set; }

        public Task LoadAsync(string filePath, bool autoPlay)
        {
            if (filePath == "error") throw new ArgumentException("Simulated error");
            if (autoPlay)
            {
                State = PlaybackState.Playing;
                StateChanged?.Invoke(this, State);
            }
            else
            {
                State = PlaybackState.Paused;
                StateChanged?.Invoke(this, State);
            }
            return Task.CompletedTask;
        }

        public void Play()
        {
            State = PlaybackState.Playing;
            StateChanged?.Invoke(this, State);
        }

        public void Pause()
        {
            State = PlaybackState.Paused;
            StateChanged?.Invoke(this, State);
        }

        public void Stop()
        {
            State = PlaybackState.Stopped;
            StateChanged?.Invoke(this, State);
        }

        public void SetRate(float rate) => Rate = rate;
        public void NextFrame() { }

        public void Seek(TimeSpan time) => LastSeekTime = time;
        public void SeekRelative(TimeSpan offset) => LastSeekRelativeOffset = offset;

        public void SetMute(bool mute) => IsMuted = mute;

        public void SimulateError(string msg) => PlaybackError?.Invoke(this, msg);

        public void SimulateStateChange(PlaybackState s)
        {
            State = s;
            StateChanged?.Invoke(this, s);
        }

        public void Dispose() { }
    }

    private class MockOutput : IOutputController
    {
        public bool IsBlackout { get; private set; }
        public void SetBlackout(bool enabled) => IsBlackout = enabled;
    }

    private class MockPrompts : IUserPromptService
    {
        public UserChoice NextChoice { get; set; } = UserChoice.Stop;
        public bool ShouldThrow { get; set; }
        public Task<UserChoice> ShowPlaybackErrorAsync(string message, string? details)
        {
            if (ShouldThrow) throw new Exception("Simulated prompt failure");
            return Task.FromResult(NextChoice);
        }
    }

    [Fact]
    public void Panic_ShouldBlackoutAndPause()
    {
        var engine = new MockEngine();
        var output = new MockOutput();
        var prompts = new MockPrompts();
        var settings = new AppSettings();
        var sm = new PlaybackStateMachine(engine, output, prompts, new AppLogger("logs"), settings);

        // Start playing
        sm.LoadAsync("test.mp4", true).Wait();
        Assert.Equal(PlaybackState.Playing, sm.State);

        // Enter Panic
        sm.TogglePanic();

        Assert.True(sm.IsPanic);
        Assert.Equal(PlaybackState.PanicBlackout, sm.State);
        Assert.True(output.IsBlackout);
        Assert.Equal(PlaybackState.Paused, engine.State); // Should force pause
    }

    [Fact]
    public void Panic_ShouldMuteAudio_WhenConfigured()
    {
        var engine = new MockEngine();
        var output = new MockOutput();
        var prompts = new MockPrompts();
        var settings = new AppSettings
        {
            PanicMutesAudio = true,
            RestoreAudioAfterPanic = true
        };
        var sm = new PlaybackStateMachine(engine, output, prompts, new AppLogger("logs"), settings);

        sm.LoadAsync("test.mp4", true).Wait();
        Assert.False(engine.IsMuted);

        sm.TogglePanic();
        Assert.True(engine.IsMuted);

        sm.TogglePanic(); // Exit
        Assert.False(engine.IsMuted); // Restored
    }

    [Fact]
    public void Panic_ShouldNotRestoreAudio_WhenConfiguredOff()
    {
        var engine = new MockEngine();
        var output = new MockOutput();
        var prompts = new MockPrompts();
        var settings = new AppSettings
        {
            PanicMutesAudio = true,
            RestoreAudioAfterPanic = false
        };
        var sm = new PlaybackStateMachine(engine, output, prompts, new AppLogger("logs"), settings);

        sm.TogglePanic();
        Assert.True(engine.IsMuted);

        sm.TogglePanic(); // Exit
        Assert.True(engine.IsMuted); // Should remain muted
    }

    [Fact]
    public void Panic_ShouldResumePlayback_WhenConfigured()
    {
        var engine = new MockEngine();
        var output = new MockOutput();
        var prompts = new MockPrompts();
        var settings = new AppSettings { ResumePlaybackAfterPanic = true };
        var sm = new PlaybackStateMachine(engine, output, prompts, new AppLogger("logs"), settings);

        sm.LoadAsync("test.mp4", true).Wait(); // Playing
        Assert.Equal(PlaybackState.Playing, engine.State);

        sm.TogglePanic();
        Assert.Equal(PlaybackState.Paused, engine.State);

        sm.TogglePanic(); // Exit
        Assert.Equal(PlaybackState.Playing, engine.State); // Resumed
    }

    [Fact]
    public void Panic_ShouldNotResume_IfWasPausedBefore()
    {
        var engine = new MockEngine();
        var output = new MockOutput();
        var prompts = new MockPrompts();
        var settings = new AppSettings { ResumePlaybackAfterPanic = true };
        var sm = new PlaybackStateMachine(engine, output, prompts, new AppLogger("logs"), settings);

        sm.LoadAsync("test.mp4", false).Wait(); // Paused
        Assert.Equal(PlaybackState.Paused, engine.State);

        sm.TogglePanic();
        sm.TogglePanic(); // Exit

        Assert.Equal(PlaybackState.Paused, engine.State); // Should stay paused
    }

    [Fact]
    public void Load_ShouldExitPanic_AndResetBlackout()
    {
        var engine = new MockEngine();
        var output = new MockOutput();
        var prompts = new MockPrompts();
        var sm = new PlaybackStateMachine(engine, output, prompts, new AppLogger("logs"), new AppSettings());

        sm.TogglePanic();
        Assert.True(sm.IsPanic);

        sm.LoadAsync("newfile.mp4", true).Wait();

        Assert.False(sm.IsPanic);
        Assert.False(output.IsBlackout);
    }

    [Fact]
    public void Error_ShouldBlackoutAndStop_WhenUserChoosesStop()
    {
        var engine = new MockEngine();
        var output = new MockOutput();
        var prompts = new MockPrompts { NextChoice = UserChoice.Stop };
        var sm = new PlaybackStateMachine(engine, output, prompts, new AppLogger("logs"), new AppSettings());

        bool errorReported = false;
        sm.ErrorOccurred += (_, __) => errorReported = true;

        // Simulate engine error
        engine.SimulateError("Codec missing");

        Assert.True(errorReported);
        Assert.True(output.IsBlackout);
        Assert.Equal(PlaybackState.Stopped, engine.State); // Should transition to Stopped eventually
    }

    [Fact]
    public void Error_ShouldReload_WhenUserChoosesRetry()
    {
        var engine = new MockEngine();
        var output = new MockOutput();
        var prompts = new MockPrompts { NextChoice = UserChoice.Retry };
        var sm = new PlaybackStateMachine(engine, output, prompts, new AppLogger("logs"), new AppSettings());

        // Initial load
        sm.LoadAsync("test.mp4", true).Wait();
        Assert.Equal(PlaybackState.Playing, engine.State);

        bool errorReported = false;
        sm.ErrorOccurred += (_, __) => errorReported = true;

        // Simulate engine error
        engine.SimulateError("Codec missing");

        Assert.True(errorReported);
        // After retry, it should load again (Load calls Play -> Playing)
        Assert.Equal(PlaybackState.Playing, engine.State);
        Assert.False(output.IsBlackout); // Should be visible again
    }

    [Fact]
    public void Seek_ShouldBeIgnored_DuringPanic()
    {
        var engine = new MockEngine();
        var output = new MockOutput();
        var prompts = new MockPrompts();
        var sm = new PlaybackStateMachine(engine, output, prompts, new AppLogger("logs"), new AppSettings());

        sm.TogglePanic();
        sm.Seek(TimeSpan.FromSeconds(10));

        Assert.Equal(TimeSpan.Zero, engine.LastSeekTime); // Not called
    }

    [Fact]
    public void Play_ShouldExitPanic()
    {
        var engine = new MockEngine();
        var output = new MockOutput();
        var prompts = new MockPrompts();
        var sm = new PlaybackStateMachine(engine, output, prompts, new AppLogger("logs"), new AppSettings());

        sm.TogglePanic();
        Assert.True(sm.IsPanic);

        sm.Play(); // Should implicitly exit panic

        Assert.False(sm.IsPanic);
        Assert.False(output.IsBlackout);
        Assert.Equal(PlaybackState.Playing, engine.State);
    }

    [Fact]
    public async Task Error_ShouldFallbackToStop_WhenPromptServiceThrows()
    {
        var engine = new MockEngine();
        var output = new MockOutput();
        var prompts = new MockPrompts { ShouldThrow = true };
        var sm = new PlaybackStateMachine(engine, output, prompts, new AppLogger("logs"), new AppSettings());

        // Initial load to have a state
        await sm.LoadAsync("test.mp4", true);
        Assert.Equal(PlaybackState.Playing, engine.State);

        // Simulate engine error which triggers OnEngineError
        // We use Task.Delay because OnEngineError is async void
        engine.SimulateError("Critical failure");

        await Task.Delay(100); // Give it a moment to process async void

        // Should fallback to UserChoice.Stop
        Assert.True(output.IsBlackout);
        Assert.Equal(PlaybackState.Stopped, engine.State);
    }
}
