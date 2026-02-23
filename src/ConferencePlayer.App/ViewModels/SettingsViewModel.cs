using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using ConferencePlayer.Core;
using ConferencePlayer.Services;
using ConferencePlayer.Utils;
using ConferencePlayer.Views;

namespace ConferencePlayer.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly AppLogger _logger;
    private readonly IFileDialogService _fileDialogs;
    private readonly DisplayService _display;
    private readonly OutputWindow _outputWindow;
    private readonly FolderWatchService _folderWatch;

    public event EventHandler? RequestClose;

    public SettingsViewModel(
        AppSettings settings,
        SettingsStore settingsStore,
        AppLogger logger,
        IFileDialogService fileDialogs,
        DisplayService display,
        OutputWindow outputWindow,
        FolderWatchService folderWatch)
    {
        _settings = settings;
        _settingsStore = settingsStore;
        _logger = logger;
        _fileDialogs = fileDialogs;
        _display = display;
        _outputWindow = outputWindow;
        _folderWatch = folderWatch;

        AvailableScreens = new ObservableCollection<string>();
        RefreshScreens();

        _display.ScreensChanged += (_, __) => Dispatcher.UIThread.Post(RefreshScreens);

        BrowseWatchFolderCommand = new RelayCommand(async () => await BrowseWatchFolderAsync());
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));
    }

    public ObservableCollection<string> AvailableScreens { get; }

    public bool WatchFolderEnabled
    {
        get => _settings.WatchFolderEnabled;
        set { _settings.WatchFolderEnabled = value; Raise(); }
    }

    public string WatchedFolderPath
    {
        get => _settings.WatchedFolderPath;
        set { _settings.WatchedFolderPath = value; Raise(); }
    }

    public bool IncludeSubfolders
    {
        get => _settings.IncludeSubfolders;
        set { _settings.IncludeSubfolders = value; Raise(); }
    }

    public bool AutoAdvancePlaylist
    {
        get => _settings.AutoAdvancePlaylist;
        set { _settings.AutoAdvancePlaylist = value; Raise(); }
    }

    public bool LoopPlaylist
    {
        get => _settings.LoopPlaylist;
        set { _settings.LoopPlaylist = value; Raise(); }
    }

    public bool PersistPlaylist
    {
        get => _settings.PersistPlaylist;
        set { _settings.PersistPlaylist = value; Raise(); }
    }

    public bool EnablePreviewWindow
    {
        get => _settings.EnablePreviewWindow;
        set { _settings.EnablePreviewWindow = value; Raise(); }
    }

    public bool PreviewCuesSelectedItem
    {
        get => _settings.PreviewCuesSelectedItem;
        set { _settings.PreviewCuesSelectedItem = value; Raise(); }
    }

    public bool PreviewAudioEnabled
    {
        get => _settings.PreviewAudioEnabled;
        set { _settings.PreviewAudioEnabled = value; Raise(); }
    }

    public bool PanicMutesAudio
    {
        get => _settings.PanicMutesAudio;
        set { _settings.PanicMutesAudio = value; Raise(); }
    }

    public bool RestoreAudioAfterPanic
    {
        get => _settings.RestoreAudioAfterPanic;
        set { _settings.RestoreAudioAfterPanic = value; Raise(); }
    }

    public bool ResumePlaybackAfterPanic
    {
        get => _settings.ResumePlaybackAfterPanic;
        set { _settings.ResumePlaybackAfterPanic = value; Raise(); }
    }

    // Hotkeys
    public string HotKey_PlayPause { get => _settings.HotKey_PlayPause; set { _settings.HotKey_PlayPause = value; Raise(); } }
    public string HotKey_Stop { get => _settings.HotKey_Stop; set { _settings.HotKey_Stop = value; Raise(); } }

    public string HotKey_PlayNext { get => _settings.HotKey_PlayNext; set { _settings.HotKey_PlayNext = value; Raise(); } }
    public string HotKey_PlayPrev { get => _settings.HotKey_PlayPrev; set { _settings.HotKey_PlayPrev = value; Raise(); } }

    public string HotKey_FrameStep { get => _settings.HotKey_FrameStep; set { _settings.HotKey_FrameStep = value; Raise(); } }

    public string HotKey_SelectNext { get => _settings.HotKey_SelectNext; set { _settings.HotKey_SelectNext = value; Raise(); } }
    public string HotKey_SelectPrev { get => _settings.HotKey_SelectPrev; set { _settings.HotKey_SelectPrev = value; Raise(); } }

    public string HotKey_SeekForward { get => _settings.HotKey_SeekForward; set { _settings.HotKey_SeekForward = value; Raise(); } }
    public string HotKey_SeekBack { get => _settings.HotKey_SeekBack; set { _settings.HotKey_SeekBack = value; Raise(); } }

    public string HotKey_IncreaseSpeed { get => _settings.HotKey_IncreaseSpeed; set { _settings.HotKey_IncreaseSpeed = value; Raise(); } }
    public string HotKey_DecreaseSpeed { get => _settings.HotKey_DecreaseSpeed; set { _settings.HotKey_DecreaseSpeed = value; Raise(); } }

    public string HotKey_Panic { get => _settings.HotKey_Panic; set { _settings.HotKey_Panic = value; Raise(); } }

    public string HotKey_AddFiles { get => _settings.HotKey_AddFiles; set { _settings.HotKey_AddFiles = value; Raise(); } }
    public string HotKey_AddFolder { get => _settings.HotKey_AddFolder; set { _settings.HotKey_AddFolder = value; Raise(); } }

    public string AppVersion => $"Project Vela v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}";

    public string PreferredOutputScreenName
    {
        get => _settings.PreferredOutputScreenName;
        set { _settings.PreferredOutputScreenName = value; Raise(); }
    }

    private string _validationMessage = string.Empty;
    public string ValidationMessage
    {
        get => _validationMessage;
        set => Set(ref _validationMessage, value);
    }

    public RelayCommand BrowseWatchFolderCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    private async Task BrowseWatchFolderAsync()
    {
        var folder = await _fileDialogs.PickFolderAsync();
        if (!string.IsNullOrWhiteSpace(folder))
            WatchedFolderPath = folder;
    }

    private bool IsKeyValid(string name, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            ValidationMessage = $"{name} hotkey cannot be empty.";
            return false;
        }
        try
        {
            Avalonia.Input.KeyGesture.Parse(key);
            return true;
        }
        catch
        {
            ValidationMessage = $"{name} hotkey '{key}' is invalid.";
            return false;
        }
    }

    private bool ValidateAllHotkeys()
    {
        if (!IsKeyValid("Play/Pause", HotKey_PlayPause)) return false;
        if (!IsKeyValid("Stop", HotKey_Stop)) return false;
        if (!IsKeyValid("Play Next", HotKey_PlayNext)) return false;
        if (!IsKeyValid("Play Prev", HotKey_PlayPrev)) return false;
        if (!IsKeyValid("Frame Step", HotKey_FrameStep)) return false;
        if (!IsKeyValid("Select Next", HotKey_SelectNext)) return false;
        if (!IsKeyValid("Select Prev", HotKey_SelectPrev)) return false;
        if (!IsKeyValid("Seek Forward", HotKey_SeekForward)) return false;
        if (!IsKeyValid("Seek Back", HotKey_SeekBack)) return false;
        if (!IsKeyValid("Speed Up", HotKey_IncreaseSpeed)) return false;
        if (!IsKeyValid("Speed Down", HotKey_DecreaseSpeed)) return false;
        if (!IsKeyValid("Panic", HotKey_Panic)) return false;
        if (!IsKeyValid("Add Files", HotKey_AddFiles)) return false;
        if (!IsKeyValid("Add Folder", HotKey_AddFolder)) return false;

        ValidationMessage = string.Empty;
        return true;
    }

    private void Save()
    {
        if (!ValidateAllHotkeys())
            return;

        // NOTE (v1): watched-folder filtering UI is deferred.
        _settingsStore.Save(_settings, _logger);

        // Apply folder watch settings.
        try
        {
            _folderWatch.Stop();
            if (_settings.WatchFolderEnabled)
            {
                _folderWatch.Start();
                _ = _folderWatch.ScanExistingAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Applying folder watch settings failed", ex);
        }

        // Apply output screen choice immediately.
        try
        {
            var screens = _display.GetAllScreens();
            var chosen = screens.FirstOrDefault(s => string.Equals(s.DisplayName, _settings.PreferredOutputScreenName, StringComparison.OrdinalIgnoreCase));
            if (chosen != null)
                _outputWindow.MoveToScreen(chosen);
        }
        catch (Exception ex)
        {
            _logger.Error("Applying output screen settings failed", ex);
        }

        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshScreens()
    {
        AvailableScreens.Clear();

        foreach (var s in _display.GetAllScreens())
        {
            if (!string.IsNullOrWhiteSpace(s.DisplayName))
                AvailableScreens.Add(s.DisplayName);
            else
                AvailableScreens.Add($"Screen {AvailableScreens.Count + 1}");
        }

        if (AvailableScreens.Count > 0 && string.IsNullOrWhiteSpace(PreferredOutputScreenName))
        {
            // Default to non-primary if available, else primary.
            var nonPrimary = _display.GetAllScreens().FirstOrDefault(x => !x.IsPrimary);
            PreferredOutputScreenName = nonPrimary?.DisplayName ?? _display.GetPrimary()?.DisplayName ?? AvailableScreens[0];
        }
    }
}
