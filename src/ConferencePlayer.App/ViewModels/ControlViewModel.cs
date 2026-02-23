using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using ConferencePlayer.Core;
using ConferencePlayer.Playback;
using ConferencePlayer.Services;
using ConferencePlayer.Utils;
using ConferencePlayer.Views;
using LibVLCSharp.Shared;

namespace ConferencePlayer.ViewModels;

public sealed class ControlViewModel : ObservableObject
{
    private readonly ControlWindow _controlWindow;
    private readonly OutputWindow _outputWindow;

    private readonly AppLogger _logger;
    private readonly AppSettings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly PlaylistStore _playlistStore;

    private readonly PlaybackStateMachine _playback;
    private readonly IPlaybackEngine _previewPlayback;
    private readonly FolderWatchService _folderWatch;
    private readonly IFileDialogService _fileDialogs;
    private readonly DisplayService _display;
    private readonly LibVLC _libVLC;

    private PlaylistItemViewModel? _selectedItem;
    private string _statusText = "Idle";
    private string _previewStatusText = "Preview: (none)";

    // We bind IsPanic to UI, but source of truth is StateMachine
    private bool _isPanic;
    private string _panicButtonText = "BLACKOUT (F12)";
    private string _panicButtonColor = "#AA0000";

    private float _selectedSpeed = 1.0f;

    public ControlViewModel(
        ControlWindow controlWindow,
        OutputWindow outputWindow,
        AppLogger logger,
        AppSettings settings,
        SettingsStore settingsStore,
        PlaylistStore playlistStore,
        PlaybackStateMachine playback,
        IPlaybackEngine previewPlayback,
        FolderWatchService folderWatch,
        IFileDialogService fileDialogs,
        DisplayService display,
        LibVLC libVLC)
    {
        _controlWindow = controlWindow;
        _outputWindow = outputWindow;
        _libVLC = libVLC;
        _logger = logger;
        _settings = settings;
        _settingsStore = settingsStore;
        _playlistStore = playlistStore;
        _playback = playback;
        _previewPlayback = previewPlayback;
        _folderWatch = folderWatch;
        _fileDialogs = fileDialogs;
        _display = display;

        Playlist = new ObservableCollection<PlaylistItemViewModel>();

        AvailableSpeeds = new ObservableCollection<float>(new[]
        {
            0.25f, 0.5f, 0.75f, 1.0f, 1.25f, 1.5f, 2.0f
        });

        // Commands
        AddFilesCommand = new RelayCommand(async () => await AddFilesAsync());
        AddFolderCommand = new RelayCommand(async () => await AddFolderAsync());
        RemoveSelectedCommand = new RelayCommand(RemoveSelected, () => SelectedItem != null);
        ClearPlaylistCommand = new RelayCommand(ClearPlaylist, () => Playlist.Count > 0);
        PlaySelectedCommand = new RelayCommand(PlaySelected, () => SelectedItem != null);
        TogglePlayPauseCommand = new RelayCommand(TogglePlayPause);
        StopCommand = new RelayCommand(Stop);

        SelectNextCommand = new RelayCommand(SelectNext);
        SelectPrevCommand = new RelayCommand(SelectPrev);
        PlayNextCommand = new RelayCommand(PlayNext);
        PlayPrevCommand = new RelayCommand(PlayPrev);

        SeekForwardCommand = new RelayCommand(SeekForward);
        SeekBackCommand = new RelayCommand(SeekBack);
        IncreaseSpeedCommand = new RelayCommand(IncreaseSpeed);
        DecreaseSpeedCommand = new RelayCommand(DecreaseSpeed);

        FrameStepCommand = new RelayCommand(FrameStep);
        PanicCommand = new RelayCommand(TogglePanic);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        OpenLogsFolderCommand = new RelayCommand(OpenLogsFolder);
        OpenHelpShortcutsCommand = new RelayCommand(OpenHelpShortcuts);
        CueNextPreviewCommand = new RelayCommand(CueNextPreview);

        // Playback events
        _playback.StateChanged += (_, s) => Dispatcher.UIThread.Post(() =>
        {
            StatusText = $"State: {s}";
            IsPanic = _playback.IsPanic;

            if (SelectedItem != null)
            {
                SelectedItem.Status = s == PlaybackState.Playing ? "Playing" :
                                      s == PlaybackState.Paused ? "Paused" :
                                      s == PlaybackState.Stopped ? "Stopped" :
                                      s == PlaybackState.PanicBlackout ? "PANIC" :
                                      s == PlaybackState.Error ? "Error" : "";
            }
            Raise(nameof(CanRemoveSelected));
        });

        _playback.EndReached += (_, __) => Dispatcher.UIThread.Post(() =>
        {
            if (SelectedItem != null) SelectedItem.Status = "Finished";
            _logger.Info("EndReached -> auto-advance check");
            if (_settings.AutoAdvancePlaylist)
            {
                if (NextInternal())
                    PlaySelected();
            }
        });

        _playback.SkipRequested += (_, __) => Dispatcher.UIThread.Post(() =>
        {
            _logger.Info("Skip requested (from error recovery)");
            if (NextInternal())
                PlaySelected();
            else
                Stop();
        });

        // Folder watch events
        _folderWatch.MediaFileDetected += (_, path) => Dispatcher.UIThread.Post(() =>
        {
            AddFiles(new[] { path });
        });

        // Screen topology changes
        _display.ScreensChanged += (_, __) => Dispatcher.UIThread.Post(() =>
        {
            ApplyMultiMonitorRules();
        });

        // Initial wiring
        // Note: PlaybackStateMachine exposes MediaPlayer for view attachment
        _outputWindow.AttachMediaPlayer(_playback.MediaPlayer);

        _controlWindow.AttachPreviewPlayer(_previewPlayback.MediaPlayer);
        // Preview is silent by default; audio monitoring can be enabled in Settings.
        _previewPlayback.SetMute(!_settings.PreviewAudioEnabled);

        // Apply monitor placement once screens are available.
        _controlWindow.Opened += (_, __) => ApplyMultiMonitorRules();
        ApplyMultiMonitorRules();

        LoadPlaylistIfEnabled();
        CueNextPreview();

        if (_settings.WatchFolderEnabled)
        {
            _folderWatch.Start();
            _ = _folderWatch.ScanExistingAsync();
        }
    }

    public ObservableCollection<PlaylistItemViewModel> Playlist { get; }

    public PlaylistItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (Set(ref _selectedItem, value))
            {
                RemoveSelectedCommand.RaiseCanExecuteChanged();
                PlaySelectedCommand.RaiseCanExecuteChanged();
                CueNextPreview();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => Set(ref _statusText, value);
    }

    public string PreviewStatusText
    {
        get => _previewStatusText;
        private set => Set(ref _previewStatusText, value);
    }

    public bool IsPanic
    {
        get => _isPanic;
        private set
        {
            if (Set(ref _isPanic, value))
            {
                PanicButtonText = _isPanic ? "RESUME (F12)" : "BLACKOUT (F12)";
                PanicButtonColor = _isPanic ? "#00AA00" : "#AA0000";
            }
        }
    }

    public string PanicButtonText
    {
        get => _panicButtonText;
        private set => Set(ref _panicButtonText, value);
    }

    public string PanicButtonColor
    {
        get => _panicButtonColor;
        private set => Set(ref _panicButtonColor, value);
    }

    public ObservableCollection<float> AvailableSpeeds { get; }

    public float SelectedSpeed
    {
        get => _selectedSpeed;
        set
        {
            if (Set(ref _selectedSpeed, value))
            {
                _playback.SetRate(value);
                _logger.Info($"Speed set: {value}");
            }
        }
    }

    // Convenience computed property (for XAML if you want)
    public bool CanRemoveSelected => SelectedItem != null;

    public string WindowTitle
    {
        get
        {
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return $"Project Vela - Control v{v?.ToString(3)}";
        }
    }

    // Commands
    public RelayCommand AddFilesCommand { get; }
    public RelayCommand AddFolderCommand { get; }
    public RelayCommand RemoveSelectedCommand { get; }
    public RelayCommand ClearPlaylistCommand { get; }
    public RelayCommand PlaySelectedCommand { get; }
    public RelayCommand TogglePlayPauseCommand { get; }
    public RelayCommand StopCommand { get; }

    public RelayCommand SelectNextCommand { get; }
    public RelayCommand SelectPrevCommand { get; }
    public RelayCommand PlayNextCommand { get; }
    public RelayCommand PlayPrevCommand { get; }

    public RelayCommand SeekForwardCommand { get; }
    public RelayCommand SeekBackCommand { get; }
    public RelayCommand IncreaseSpeedCommand { get; }
    public RelayCommand DecreaseSpeedCommand { get; }

    public RelayCommand FrameStepCommand { get; }
    public RelayCommand PanicCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand OpenLogsFolderCommand { get; }
    public RelayCommand OpenHelpShortcutsCommand { get; }
    public RelayCommand CueNextPreviewCommand { get; }

    public void AddFiles(IEnumerable<string> paths, bool saveAfter = true)
    {
        var added = 0;
        foreach (var p in paths)
        {
            if (string.IsNullOrWhiteSpace(p))
                continue;

            if (!File.Exists(p))
                continue;

            // Avoid duplicates
            if (Playlist.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x.FilePath, p)))
                continue;

            var newItem = new PlaylistItemViewModel(new PlaylistItem(p));
            Playlist.Add(newItem);
            _ = LoadDurationAsync(newItem);
            added++;
        }

        if (added > 0 && SelectedItem == null)
        {
            SelectedItem = Playlist.FirstOrDefault();
        }

        ClearPlaylistCommand.RaiseCanExecuteChanged();

        if (saveAfter)
            SavePlaylistIfEnabled();

        _logger.Info($"AddFiles: added={added}");
    }

    private async Task LoadDurationAsync(PlaylistItemViewModel item)
    {
        try
        {
            // Run on thread pool to avoid blocking UI
            await Task.Run(() =>
            {
                using var media = new Media(_libVLC, item.FilePath);
                media.Parse(MediaParseOptions.ParseLocal);
                var ms = media.Duration;
                if (ms > 0)
                {
                    var ts = TimeSpan.FromMilliseconds(ms);
                    Dispatcher.UIThread.Post(() => item.Duration = ts.ToString(@"mm\:ss"));
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load duration for {item.FilePath}", ex);
        }
    }

    private void SavePlaylistIfEnabled()
    {
        if (!_settings.PersistPlaylist)
            return;

        _playlistStore.Save(Playlist.Select(x => x.Model).ToList(), _logger);
    }

    private void LoadPlaylistIfEnabled()
    {
        if (!_settings.PersistPlaylist)
            return;

        var items = _playlistStore.Load(_logger);
        if (items.Count == 0)
            return;

        // Don't re-save while loading.
        AddFiles(items.Select(x => x.FilePath), saveAfter: false);
    }

    private async Task AddFilesAsync()
    {
        try
        {
            var files = await _fileDialogs.PickMediaFilesAsync();
            AddFiles(files);
        }
        catch (Exception ex)
        {
            _logger.Error("AddFilesAsync failed", ex);
        }
    }

    private async Task AddFolderAsync()
    {
        try
        {
            var folder = await _fileDialogs.PickFolderAsync();
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                return;

            // Add all matching files in folder (and subfolders if enabled in settings).
            var option = _settings.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var filter = new MediaFileFilter(_settings);

            var files = Directory
                .EnumerateFiles(folder, "*.*", option)
                .Where(filter.IsAllowed)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            AddFiles(files);
        }
        catch (Exception ex)
        {
            _logger.Error("AddFolderAsync failed", ex);
        }
    }

    private void RemoveSelected()
    {
        if (SelectedItem == null)
            return;

        var idx = Playlist.IndexOf(SelectedItem);
        Playlist.Remove(SelectedItem);

        if (Playlist.Count == 0)
        {
            SelectedItem = null;
        }
        else
        {
            idx = Math.Clamp(idx, 0, Playlist.Count - 1);
            SelectedItem = Playlist[idx];
        }

        ClearPlaylistCommand.RaiseCanExecuteChanged();
        _logger.Info("Removed selected item.");
        SavePlaylistIfEnabled();
    }

    private void ClearPlaylist()
    {
        Playlist.Clear();
        SelectedItem = null;
        ClearPlaylistCommand.RaiseCanExecuteChanged();
        _logger.Info("Playlist cleared.");
        SavePlaylistIfEnabled();
    }

    private void PlaySelected()
    {
        if (SelectedItem == null)
            return;

        // Exceptions handled inside StateMachine.Load -> OnEngineError
        _playback.Load(SelectedItem.FilePath, autoPlay: true);

        // Re-apply speed preference
        _playback.SetRate(SelectedSpeed);

        StatusText = $"Playing: {SelectedItem.Name}";
        CueNextPreview();
    }

    private void TogglePlayPause()
    {
        // Best-effort: if playing -> pause else play
        if (_playback.State == PlaybackState.Playing)
        {
            _playback.Pause();
        }
        else
        {
            _playback.Play();
        }
    }

    private void Stop()
    {
        _playback.Stop();
        StatusText = "Stopped";
    }

    private bool NextInternal()
    {
        if (Playlist.Count == 0)
            return false;

        var idx = SelectedItem == null ? -1 : Playlist.IndexOf(SelectedItem);

        if (idx >= Playlist.Count - 1)
        {
            if (_settings.LoopPlaylist)
            {
                SelectedItem = Playlist[0];
                StatusText = "Looped to start";
                return true;
            }

            StatusText = "End of playlist";
            return false;
        }

        SelectedItem = Playlist[idx + 1];
        return true;
    }

    private void SelectNext()
    {
        NextInternal();
    }

    private void PlayNext()
    {
        if (NextInternal())
            PlaySelected();
    }

    private bool PrevInternal()
    {
        if (Playlist.Count == 0)
            return false;

        var idx = SelectedItem == null ? 0 : Playlist.IndexOf(SelectedItem);

        if (idx <= 0)
        {
            StatusText = "Start of playlist";
            return false;
        }

        SelectedItem = Playlist[idx - 1];
        return true;
    }

    private void SelectPrev()
    {
        PrevInternal();
    }

    private void PlayPrev()
    {
        if (PrevInternal())
            PlaySelected();
    }

    private void SeekForward() => _playback.SeekRelative(TimeSpan.FromSeconds(10));

    private void SeekBack() => _playback.SeekRelative(TimeSpan.FromSeconds(-10));

    private void IncreaseSpeed()
    {
        var idx = AvailableSpeeds.IndexOf(SelectedSpeed);
        if (idx >= 0 && idx < AvailableSpeeds.Count - 1)
            SelectedSpeed = AvailableSpeeds[idx + 1];
    }

    private void DecreaseSpeed()
    {
        var idx = AvailableSpeeds.IndexOf(SelectedSpeed);
        if (idx > 0)
            SelectedSpeed = AvailableSpeeds[idx - 1];
    }

    private void FrameStep()
    {
        _playback.NextFrame();
    }

    private PlaylistItemViewModel? GetNextItemForPreview()
    {
        if (Playlist.Count == 0)
            return null;

        // Optional cue mode: preview can cue the selected item instead of the next item.
        if (_settings.PreviewCuesSelectedItem)
        {
            return SelectedItem ?? Playlist.FirstOrDefault();
        }

        var idx = SelectedItem == null ? -1 : Playlist.IndexOf(SelectedItem);
        if (idx < 0)
            idx = 0;

        if (idx >= Playlist.Count - 1)
        {
            if (_settings.LoopPlaylist)
                return Playlist[0];

            return null;
        }

        return Playlist[idx + 1];
    }

    private void CueNextPreview()
    {
        if (!_settings.EnablePreviewWindow)
            return;

        var next = GetNextItemForPreview();
        if (next == null)
        {
            PreviewStatusText = "Preview: (none)";
            _previewPlayback.Stop();
            return;
        }

        try
        {
            _previewPlayback.SetMute(!_settings.PreviewAudioEnabled);
            _previewPlayback.Load(next.FilePath, autoPlay: false);

            var mode = _settings.PreviewCuesSelectedItem ? "Selected" : "Next";
            PreviewStatusText = $"Preview ({mode}): {next.Name}";
        }
        catch (Exception ex)
        {
            _logger.Error("CueNextPreview failed", ex);
            PreviewStatusText = "Preview: (error)";
        }
    }

    private void TogglePanic()
    {
        _playback.TogglePanic();
    }

    private void OpenSettings()
    {
        var vm = new SettingsViewModel(
            settings: _settings,
            settingsStore: _settingsStore,
            logger: _logger,
            fileDialogs: _fileDialogs,
            display: _display,
            outputWindow: _outputWindow,
            folderWatch: _folderWatch);

        var win = new SettingsWindow { DataContext = vm };
        vm.RequestClose += (_, __) => win.Close();

        // Apply settings after the window closes (Save or Cancel). Safe either way.
        win.Closed += (_, __) =>
        {
            try
            {
                ApplyMultiMonitorRules();
                ApplyPreviewAudioSetting();
                CueNextPreview();
                _controlWindow.ApplyHotkeys(_settings);
            }
            catch (Exception ex)
            {
                _logger.Error("Post-settings apply failed", ex);
            }
        };

        win.ShowDialog(_controlWindow);
    }

    private void ApplyPreviewAudioSetting()
    {
        try
        {
            _previewPlayback.SetMute(!_settings.PreviewAudioEnabled);
        }
        catch (Exception ex)
        {
            _logger.Error("Applying preview audio setting failed", ex);
        }
    }

    private void OpenHelpShortcuts()
    {
        var vm = new HelpShortcutsViewModel(_settings);
        var win = new HelpShortcutsWindow { DataContext = vm };
        win.ShowDialog(_controlWindow);
    }

    private void OpenLogsFolder()
    {
        try
        {
            var folder = _settings.LogsFolderPath;
            if (string.IsNullOrWhiteSpace(folder))
                folder = PathHelpers.GetDefaultLogsFolder();

            Directory.CreateDirectory(folder);

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = folder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.Error("OpenLogsFolder failed", ex);
        }
    }

    private void ApplyMultiMonitorRules()
    {
        var screens = _display.GetAllScreens();
        var primary = _display.GetPrimary();

        // If only one monitor: keep UI on top.
        _controlWindow.Topmost = screens.Count < 2;

        // Choose output screen: preferred by name, else 2nd screen, else primary.
        var chosen = screens.FirstOrDefault(s => !string.IsNullOrWhiteSpace(_settings.PreferredOutputScreenName)
                                                && string.Equals(s.DisplayName, _settings.PreferredOutputScreenName, StringComparison.OrdinalIgnoreCase));

        if (chosen == null && screens.Count >= 2)
        {
            // Best-effort: choose the non-primary screen for output.
            chosen = screens.FirstOrDefault(s => !s.IsPrimary) ?? screens[1];
        }

        chosen ??= primary;

        if (chosen != null)
        {
            _outputWindow.MoveToScreen(chosen);
        }
    }
}
