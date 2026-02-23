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

        public PlaybackState State { get; private set; } = PlaybackState.Idle;

        public bool IsMuted { get; private set; }
        public float Rate { get; private set; } = 1.0f;

        public event EventHandler? EndReached;
        public event EventHandler<string>? PlaybackError;
        public event EventHandler<PlaybackState>? StateChanged;

        public void Load(string filePath, bool autoPlay)
        {
            if (filePath == "error") throw new ArgumentException("Simulated error");
            if (autoPlay) SetState(PlaybackState.Playing);
            else SetState(PlaybackState.Paused);
        }

        public void Play() => SetState(PlaybackState.Playing);
        public void Pause() => SetState(PlaybackState.Paused);
        public void Stop() => SetState(PlaybackState.Stopped);
        public void SetRate(float rate) => Rate = rate;
        public void NextFrame() { }
        public void SetMute(bool mute) => IsMuted = mute;

        public void SimulateError(string msg) => PlaybackError?.Invoke(this, msg);

        private void SetState(PlaybackState s)
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
        public Task<UserChoice> ShowPlaybackErrorAsync(string message, string? details)
        {
            return Task.FromResult(NextChoice);
        }
    }

    [Fact]
    public void Panic_ShouldBlackoutAndPause()
    {
        var engine = new MockEngine();
        var output = new MockOutput();
        var prompts = new MockPrompts();
        var sm = new PlaybackStateMachine(engine, output, prompts, new AppLogger("logs"), new AppSettings());

        sm.Load("test.mp4", true); // Playing
        Assert.Equal(PlaybackState.Playing, sm.State);
        Assert.False(output.IsBlackout);

        sm.TogglePanic();

        Assert.True(sm.IsPanic);
        Assert.Equal(PlaybackState.PanicBlackout, sm.State);
        Assert.True(output.IsBlackout);
        Assert.Equal(PlaybackState.Paused, engine.State); // Engine paused
    }

    [Fact]
    public void Load_ShouldTriggerError_OnException()
    {
        var engine = new MockEngine();
        var output = new MockOutput();
        var prompts = new MockPrompts();
        var sm = new PlaybackStateMachine(engine, output, prompts, new AppLogger("logs"), new AppSettings());

        bool errorTriggered = false;
        sm.ErrorOccurred += (_, __) => errorTriggered = true;

        sm.Load("error", true);

        Assert.True(errorTriggered);
        Assert.True(output.IsBlackout); // Safety state
    }

    [Fact]
    public void Load_ShouldExitPanic()
    {
        var engine = new MockEngine();
        var output = new MockOutput();
        var prompts = new MockPrompts();
        var sm = new PlaybackStateMachine(engine, output, prompts, new AppLogger("logs"), new AppSettings());

        sm.TogglePanic();
        Assert.True(sm.IsPanic);

        sm.Load("test.mp4", true);

        Assert.False(sm.IsPanic);
        Assert.False(output.IsBlackout);
    }
}
