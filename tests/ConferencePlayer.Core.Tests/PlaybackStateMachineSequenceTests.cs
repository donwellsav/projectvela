using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using ConferencePlayer.Core;
using ConferencePlayer.Playback;
using LibVLCSharp.Shared;

namespace ConferencePlayer.Core.Tests;

public class PlaybackStateMachineSequenceTests
{
    private class SequenceMockEngine : IPlaybackEngine
    {
        public List<string> CallLog { get; }

        public SequenceMockEngine(List<string> log)
        {
            CallLog = log;
        }

        public MediaPlayer MediaPlayer => throw new NotImplementedException();
        public long Time => 0;
        public PlaybackState State { get; private set; } = PlaybackState.Idle;
        public bool IsMuted { get; private set; }
        public float Rate { get; private set; } = 1.0f;

        public event EventHandler? EndReached;
        public event EventHandler<string>? PlaybackError;
        public event EventHandler<PlaybackState>? StateChanged;

        public void Load(string filePath, bool autoPlay) { }
        public void Play()
        {
            CallLog.Add("Engine.Play");
            State = PlaybackState.Playing;
        }
        public void Pause()
        {
            CallLog.Add("Engine.Pause");
            State = PlaybackState.Paused;
        }
        public void Stop()
        {
            CallLog.Add("Engine.Stop");
            State = PlaybackState.Stopped;
        }
        public void SetRate(float rate) { }
        public void NextFrame() { }
        public void Seek(TimeSpan time) { }
        public void SeekRelative(TimeSpan offset) { }
        public void SetMute(bool mute) => CallLog.Add($"Engine.SetMute({mute})");

        public void SimulateError(string msg) => PlaybackError?.Invoke(this, msg);
        public void SimulateStateChange(PlaybackState s)
        {
            State = s;
            StateChanged?.Invoke(this, s);
        }

        public void Dispose() { }
    }

    private class SequenceMockOutput : IOutputController
    {
        public List<string> CallLog { get; }

        public SequenceMockOutput(List<string> log)
        {
            CallLog = log;
        }

        public void SetBlackout(bool enabled)
        {
            CallLog.Add($"Output.SetBlackout({enabled})");
        }
    }

    private class MockPrompts : IUserPromptService
    {
        public Task<UserChoice> ShowPlaybackErrorAsync(string message, string? details)
        {
            return Task.FromResult(UserChoice.Stop);
        }
    }

    [Fact]
    public void Panic_Should_Call_Blackout_BEFORE_Pause()
    {
        // Arrange
        var log = new List<string>();
        var engine = new SequenceMockEngine(log);
        var output = new SequenceMockOutput(log);
        var prompts = new MockPrompts();
        var sm = new PlaybackStateMachine(engine, output, prompts, new AppLogger("logs"), new AppSettings());

        // Act
        sm.TogglePanic();

        // Assert
        // We expect: "Output.SetBlackout(True)", then "Engine.Pause", then maybe "Engine.SetMute(True)"
        Assert.Contains("Output.SetBlackout(True)", log);
        Assert.Contains("Engine.Pause", log);

        var blackoutIndex = log.IndexOf("Output.SetBlackout(True)");
        var pauseIndex = log.IndexOf("Engine.Pause");

        Assert.True(blackoutIndex < pauseIndex,
            $"Expected Blackout (index {blackoutIndex}) to be BEFORE Pause (index {pauseIndex}) to prevent frozen frames.");
    }

    [Fact]
    public void Error_Should_Call_Blackout_BEFORE_Pause()
    {
        // Arrange
        var log = new List<string>();
        var engine = new SequenceMockEngine(log);
        var output = new SequenceMockOutput(log);
        var prompts = new MockPrompts();
        var sm = new PlaybackStateMachine(engine, output, prompts, new AppLogger("logs"), new AppSettings());

        // Act
        engine.SimulateError("Critical Error");

        // Assert
        Assert.Contains("Output.SetBlackout(True)", log);
        Assert.Contains("Engine.Pause", log);

        var blackoutIndex = log.IndexOf("Output.SetBlackout(True)");
        var pauseIndex = log.IndexOf("Engine.Pause");

        Assert.True(blackoutIndex < pauseIndex,
            $"Expected Blackout (index {blackoutIndex}) to be BEFORE Pause (index {pauseIndex}) to ensure error screen is black.");
    }
}
