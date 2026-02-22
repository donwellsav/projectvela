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
    private readonly PreviewWindow _previewWindow;
    private readonly FolderWatchService _folderWatch;

    private string _extensionsText;

    public event EventHandler? RequestClose;

    public SettingsViewModel(
        AppSettings settings,
        SettingsStore settingsStore,
        AppLogger logger,
        IFileDialogService fileDialogs,
        DisplayService display,
        OutputWindow outputWindow,
        PreviewWindow previewWindow,
        FolderWatchService folderWatch)
    {
        _settings = settings;
        _settingsStore = settingsStore;
        _logger = logger;
        _fileDialogs = fileDialogs;
        _display = display;
        _outputWindow = outputWindow;
        _previewWindow = previewWindow;
        _folderWatch = folderWatch;

        AvailableScreens = new ObservableCollection<string>();
        RefreshScreens();

        _display.ScreensChanged += (_, __) => Dispatcher.UIThread.Post(RefreshScreens);

        _extensionsText = string.Join(";", _settings.AllowedExtensions);

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

    public bool FilterEnabled
    {
        get => _settings.FilterEnabled;
        set { _settings.FilterEnabled = value; Raise(); }
    }

    public string AllowedExtensionsText
    {
        get => _extensionsText;
        set => Set(ref _extensionsText, value);
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

    public string PreferredOutputScreenName
    {
        get => _settings.PreferredOutputScreenName;
        set { _settings.PreferredOutputScreenName = value; Raise(); }
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

    private void Save()
    {
        // Parse extensions from text box.
        var parsed = AllowedExtensionsText
            .Split(new[] { ';', ',', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Select(x => x.StartsWith(".") ? x : "." + x)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _settings.AllowedExtensions = parsed;
        _settings.EnsureDefaults();

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

        // Apply preview window visibility immediately.
        try
        {
            if (_settings.EnablePreviewWindow)
                _previewWindow.Show();
            else
                _previewWindow.Hide();
        }
        catch (Exception ex)
        {
            _logger.Error("Applying preview window settings failed", ex);
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
