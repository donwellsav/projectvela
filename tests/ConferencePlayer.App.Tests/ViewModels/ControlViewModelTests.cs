using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform;
using ConferencePlayer.Core;
using ConferencePlayer.Playback;
using ConferencePlayer.Services;
using ConferencePlayer.ViewModels;
using ConferencePlayer.Views;
using LibVLCSharp.Shared;
using Moq;
using Xunit;

namespace ConferencePlayer.App.Tests.ViewModels;

public class ControlViewModelTests
{
    private readonly Mock<IControlWindow> _mockControlWindow;
    private readonly Mock<IOutputWindow> _mockOutputWindow;
    private readonly Mock<IPlaybackEngine> _mockPreviewEngine;
    private readonly Mock<IPlaybackEngine> _mockMainEngine;
    private readonly Mock<IOutputController> _mockOutputController;
    private readonly Mock<IUserPromptService> _mockPromptService;
    private readonly Mock<IFileDialogService> _mockFileDialogs;
    private readonly Mock<IDisplayService> _mockDisplayService;

    private readonly AppLogger _logger;
    private readonly AppSettings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly PlaylistStore _playlistStore;
    private readonly PlaybackStateMachine _playbackStateMachine;
    private readonly FolderWatchService _folderWatch;
    private readonly string _tempDir;

    public ControlViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _logger = new AppLogger(_tempDir);
        _settings = new AppSettings();
        _settingsStore = new SettingsStore(Path.Combine(_tempDir, "settings.json"));
        _playlistStore = new PlaylistStore(Path.Combine(_tempDir, "playlist.json"));

        _mockControlWindow = new Mock<IControlWindow>();
        _mockOutputWindow = new Mock<IOutputWindow>();
        _mockPreviewEngine = new Mock<IPlaybackEngine>();
        _mockMainEngine = new Mock<IPlaybackEngine>();
        _mockOutputController = new Mock<IOutputController>();
        _mockPromptService = new Mock<IUserPromptService>();
        _mockFileDialogs = new Mock<IFileDialogService>();
        _mockDisplayService = new Mock<IDisplayService>();
        _mockDisplayService.Setup(x => x.GetAllScreens()).Returns(new List<Screen>());
        _mockDisplayService.Setup(x => x.GetPrimary()).Returns((Screen?)null);

        // Setup Main Engine defaults
        _mockMainEngine.Setup(x => x.State).Returns(PlaybackState.Stopped);
        _mockMainEngine.Setup(x => x.IsMuted).Returns(false);
        _mockMainEngine.Setup(x => x.Rate).Returns(1.0f);
        _mockMainEngine.Setup(x => x.Time).Returns(0);

        // Setup Preview Engine defaults
        _mockPreviewEngine.Setup(x => x.State).Returns(PlaybackState.Stopped);
        _mockPreviewEngine.Setup(x => x.IsMuted).Returns(true); // Default safety mute

        _playbackStateMachine = new PlaybackStateMachine(
            _mockMainEngine.Object,
            _mockOutputController.Object,
            _mockPromptService.Object,
            _logger,
            _settings);

        _folderWatch = new FolderWatchService(_settings, _logger);
    }

    private ControlViewModel CreateViewModel()
    {
        // We pass null for LibVLC as we don't test duration loading which requires native libs.
        // The VM catches exceptions in LoadDurationAsync so it won't crash.
        return new ControlViewModel(
            _mockControlWindow.Object,
            _mockOutputWindow.Object,
            _logger,
            _settings,
            _settingsStore,
            _playlistStore,
            _playbackStateMachine,
            _mockPreviewEngine.Object,
            _folderWatch,
            _mockFileDialogs.Object,
            _mockDisplayService.Object,
            null!);
    }

    [Fact]
    public void CueNextPreview_LoadsNextItem_Paused()
    {
        // Arrange
        var vm = CreateViewModel();
        _settings.EnablePreviewWindow = true;
        _settings.PreviewCuesSelectedItem = false; // Default: Next
        _settings.PreviewAudioEnabled = false;

        var file1 = Path.Combine(_tempDir, "1.mp4");
        var file2 = Path.Combine(_tempDir, "2.mp4");
        File.WriteAllText(file1, "dummy");
        File.WriteAllText(file2, "dummy");

        // Act
        vm.AddFiles(new[] { file1, file2 });
        vm.SelectedItem = vm.Playlist[0]; // Select first

        // Trigger cue (usually happens on selection change, verified below)
        // AddFiles sets selection to first item if null, which triggers CueNextPreview.
        // We verified AddFiles calls CueNextPreview implicitly via Property Changed.

        // Assert
        // Expect loading the NEXT item (file2) with autoPlay: false
        _mockPreviewEngine.Verify(x => x.Load(file2, false), Times.AtLeastOnce);

        // Expect Mute to be TRUE (since PreviewAudioEnabled is false)
        _mockPreviewEngine.Verify(x => x.SetMute(true), Times.AtLeastOnce);
    }

    [Fact]
    public void CueSelected_LoadsSelectedItem()
    {
        // Arrange
        var vm = CreateViewModel();
        _settings.EnablePreviewWindow = true;
        _settings.PreviewCuesSelectedItem = true; // Selected

        var file1 = Path.Combine(_tempDir, "1.mp4");
        var file2 = Path.Combine(_tempDir, "2.mp4");
        File.WriteAllText(file1, "dummy");
        File.WriteAllText(file2, "dummy");

        // Act
        vm.AddFiles(new[] { file1, file2 });
        vm.SelectedItem = vm.Playlist[0];

        // Assert
        // Expect loading the SELECTED item (file1)
        _mockPreviewEngine.Verify(x => x.Load(file1, false), Times.AtLeastOnce);
    }

    [Fact]
    public void TogglePreviewAudio_TogglesMute()
    {
        // Arrange
        var vm = CreateViewModel();
        _settings.PreviewAudioEnabled = false; // Start muted

        // Act
        vm.TogglePreviewAudioCommand.Execute(null);

        // Assert
        Assert.True(vm.IsPreviewAudioEnabled);
        // Verify engine was unmuted (SetMute(false))
        _mockPreviewEngine.Verify(x => x.SetMute(false), Times.Once);

        // Act 2
        vm.TogglePreviewAudioCommand.Execute(null);

        // Assert 2
        Assert.False(vm.IsPreviewAudioEnabled);
        // Verify engine was muted (SetMute(true))
        _mockPreviewEngine.Verify(x => x.SetMute(true), Times.AtLeastOnce);
    }

    [Fact]
    public void Panic_PausesAndMutesPreview()
    {
        // Arrange
        var vm = CreateViewModel();
        _settings.PreviewAudioEnabled = true; // Audio ON initially

        // Act
        vm.PanicCommand.Execute(null);

        // Assert
        // Note: vm.IsPanic updates via Dispatcher (async), so we check the source of truth
        Assert.True(_playbackStateMachine.IsPanic);

        // Preview should be muted and paused
        _mockPreviewEngine.Verify(x => x.SetMute(true), Times.AtLeastOnce);
        _mockPreviewEngine.Verify(x => x.Pause(), Times.AtLeastOnce);
    }

    [Fact]
    public void ExitPanic_RestoresPreviewAudio_IfEnabled()
    {
        // Arrange
        var vm = CreateViewModel();
        _settings.PreviewAudioEnabled = true;

        // Enter panic
        vm.PanicCommand.Execute(null);
        _mockPreviewEngine.Invocations.Clear(); // Clear previous calls

        // Act
        vm.PanicCommand.Execute(null); // Toggle off

        // Assert
        Assert.False(_playbackStateMachine.IsPanic);
        // Should restore audio (SetMute(false))
        _mockPreviewEngine.Verify(x => x.SetMute(false), Times.Once);
    }

    [Fact]
    public void ExitPanic_KeepsPreviewMuted_IfDisabled()
    {
        // Arrange
        var vm = CreateViewModel();
        _settings.PreviewAudioEnabled = false;

        // Enter panic
        vm.PanicCommand.Execute(null);
        _mockPreviewEngine.Invocations.Clear();

        // Act
        vm.PanicCommand.Execute(null); // Toggle off

        // Assert
        Assert.False(_playbackStateMachine.IsPanic);
        // Should ensure mute is true
        _mockPreviewEngine.Verify(x => x.SetMute(true), Times.AtLeastOnce);
    }
}
