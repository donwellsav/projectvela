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

namespace ConferencePlayer.ViewModels;

public sealed class ControlViewModel : ObservableObject
{
    private readonly ControlWindow _controlWindow;
    private readonly OutputWindow _outputWindow;
    private readonly PreviewWindow _previewWindow;


    private readonly AppLogger _logger;
    private readonly AppSettings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly PlaylistStore _playlistStore;

    private readonly IPlaybackEngine _playback;
    private readonly IPlaybackEngine _previewPlayback;
    private readonly FolderWatchService _folderWatch;
    private readonly IFileDialogService _fileDialogs;
    private readonly IUserPromptService _prompts;
    private readonly DisplayService _display;

    private PlaylistItem? _selectedItem;
    private string _statusText = "Idle";
    private string _previewStatusText = "Preview: (none)";
    private bool _isPanic;

    private float _selectedSpeed = 1.0f;

    public ControlViewModel(
        ControlWindow controlWindow,
        OutputWindow outputWindow,
        PreviewWindow previewWindow,
        AppLogger logger,
        AppSettings settings,
        SettingsStore settingsStore,
        PlaylistStore playlistStore,
        IPlaybackEngine playback,
        IPlaybackEngine previewPlayback,
        FolderWatchService folderWatch,
        IFileDialogService fileDialogs,
        IUserPromptService prompts,
        DisplayService display)
    {
        _controlWindow = controlWindow;
        _outputWindow = outputWindow;
        _previewWindow = previewWindow;
        _logger = logger;
        _settings = settings;
        _settingsStore = settingsStore;
        _playlistStore = playlistStore;
        _playback = playback;
        _previewPlayback = previewPlayback;
        _folderWatch = folderWatch;
        _fileDialogs = fileDialogs;
        _prompts = prompts;
        _display = display;

        try { _previewWindow.Owner = _controlWindow; } catch { /* best-effort */ }

        Playlist = new ObservableCollection<PlaylistItem>();

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
        NextCommand = new RelayCommand(Next);
        PrevCommand = new RelayCommand(Prev);
        FrameStepCommand = new RelayCommand(FrameStep);
        PanicCommand = new RelayCommand(TogglePanic);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        OpenLogsFolderCommand = new RelayCommand(OpenLogsFolder);
        CueNextPreviewCommand = new RelayCommand(CueNextPreview);
        TogglePreviewWindowCommand = new RelayCommand(TogglePreviewWindow);

        // Playback events
        _playback.StateChanged += (_, s) => Dispatcher.UIThread.Post(() =>
        {
            StatusText = $"State: {s}";
            Raise(nameof(CanRemoveSelected));
        });

        _playback.EndReached += (_, __) => Dispatcher.UIThread.Post(async () =>
        {
            _logger.Info("EndReached -> auto-advance check");
            if (_settings.AutoAdvancePlaylist)
            {
                if (NextInternal())
                    PlaySelected();
            }
        });

        _playback.PlaybackError += (_, msg) => Dispatcher.UIThread.Post(async () =>
        {
            await HandlePlaybackErrorAsync(msg);
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
        _outputWindow.AttachMediaPlayer(_playback.MediaPlayer);
        _previewWindow.AttachMediaPlayer(_previewPlayback.MediaPlayer);
        // Preview is silent by default; audio monitoring can be enabled in Settings.
        _previewPlayback.SetMute(!_settings.PreviewAudioEnabled);
        _previewWindow.SetBlackout(true);

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

    public ObservableCollection<PlaylistItem> Playlist { get; }

    public PlaylistItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (Set(ref _selectedItem, value))
            {
                RemoveSelectedCommand.RaiseCanExecuteChanged();
                PlaySelectedCommand.RaiseCanExecuteChanged();
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
        private set => Set(ref _isPanic, value);
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

    // Commands
    public RelayCommand AddFilesCommand { get; }
    public RelayCommand AddFolderCommand { get; }
    public RelayCommand RemoveSelectedCommand { get; }
    public RelayCommand ClearPlaylistCommand { get; }
    public RelayCommand PlaySelectedCommand { get; }
    public RelayCommand TogglePlayPauseCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand PrevCommand { get; }
    public RelayCommand FrameStepCommand { get; }
    public RelayCommand PanicCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand OpenLogsFolderCommand { get; }
    public RelayCommand CueNextPreviewCommand { get; }
    public RelayCommand TogglePreviewWindowCommand { get; }

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

            Playlist.Add(new PlaylistItem(p));
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

    private void SavePlaylistIfEnabled()
    {
        if (!_settings.PersistPlaylist)
            return;

        _playlistStore.Save(Playlist, _logger);
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
            await HandlePlaybackErrorAsync(ex.Message);
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
            await HandlePlaybackErrorAsync(ex.Message);
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

        try
        {
            IsPanic = false;
            _outputWindow.SetBlackout(false);

            _playback.Load(SelectedItem.FilePath, autoPlay: true);
            SelectedSpeed = SelectedSpeed; // re-apply
            StatusText = $"Playing: {SelectedItem.DisplayName}";
            CueNextPreview();
        }
        catch (Exception ex)
        {
            _logger.Error("PlaySelected failed", ex);
            _ = HandlePlaybackErrorAsync(ex.Message);
        }
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
        IsPanic = false;
        _outputWindow.SetBlackout(true);
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

    private void Next()
    {
        NextInternal();
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

    private void Prev()
    {
        PrevInternal();
    }

    private void FrameStep()
    {
        // Frame stepping usually expects paused playback.
        _playback.Pause();
        _playback.NextFrame();
    }

    private void TogglePreviewWindow()
    {
        if (_previewWindow.IsVisible)
            _previewWindow.Hide();
        else
            _previewWindow.Show();
    }

    private PlaylistItem? GetNextItemForPreview()
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
            _previewWindow.SetBlackout(true);
            _previewPlayback.Stop();
            return;
        }

        try
        {
            _previewPlayback.SetMute(!_settings.PreviewAudioEnabled);
            _previewPlayback.Load(next.FilePath, autoPlay: false);
            _previewWindow.SetBlackout(false);

            var mode = _settings.PreviewCuesSelectedItem ? "Selected" : "Next";
            PreviewStatusText = $"Preview ({mode}): {next.DisplayName}";
        }
        catch (Exception ex)
        {
            _logger.Error("CueNextPreview failed", ex);
            PreviewStatusText = "Preview: (error)";
            _previewWindow.SetBlackout(true);
        }
    }

    private bool _savedMuteBeforePanic;
    private bool _wasPlayingBeforePanic;

    private void TogglePanic()
    {
        IsPanic = !IsPanic;

        if (IsPanic)
        {
            _logger.Warn("PANIC ON");
            _savedMuteBeforePanic = _playback.IsMuted;
            _wasPlayingBeforePanic = _playback.State == PlaybackState.Playing;

            // Safety posture: pause playback while output is black.
            _playback.Pause();

            _outputWindow.SetBlackout(true);

            if (_settings.PanicMutesAudio)
                _playback.SetMute(true);
        }
        else
        {
            _logger.Warn("PANIC OFF");

            // Exit blackout first. Default behavior is to remain paused until operator hits Play.
            _outputWindow.SetBlackout(false);

            if (_settings.RestoreAudioAfterPanic)
                _playback.SetMute(_savedMuteBeforePanic);

            if (_settings.ResumePlaybackAfterPanic && _wasPlayingBeforePanic)
                _playback.Play();
        }
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
            previewWindow: _previewWindow,
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

    private async Task HandlePlaybackErrorAsync(string message)
    {
        _logger.Warn($"Playback error: {message}");

        // Output goes black and stays black until operator chooses.
        _outputWindow.SetBlackout(true);

        var choice = await _prompts.ShowPlaybackErrorAsync(
            message: message,
            details: $"Log file: {_logger.LogFilePath}");

        _logger.Info($"Operator choice after error: {choice}");

        switch (choice)
        {
            case UserChoice.Retry:
                PlaySelected();
                break;
            case UserChoice.Skip:
                if (NextInternal())
                    PlaySelected();
                else
                    Stop();
                break;
            case UserChoice.Stop:
            default:
                Stop();
                break;
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
