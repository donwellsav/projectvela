using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ConferencePlayer.Core;
using ConferencePlayer.Playback;
using ConferencePlayer.Services;
using ConferencePlayer.ViewModels;
using ConferencePlayer.Views;

namespace ConferencePlayer;

public partial class App : Application
{
    private AppLogger? _logger;
    private FolderWatchService? _folderWatch;
    private IPlaybackEngine? _playback;
    private IPlaybackEngine? _previewPlayback;
    private DisplayService? _displayService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Crash posture: always log.
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                try
                {
                    if (e.ExceptionObject is Exception ex)
                        _logger?.Error("Unhandled exception (AppDomain).", ex);
                    else
                        _logger?.Warn($"Unhandled exception (AppDomain): {e.ExceptionObject}");
                }
                catch { /* ignore */ }
            };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                try
                {
                    _logger?.Error("Unobserved task exception.", e.Exception);
                }
                catch { /* ignore */ }
            };

            // Load settings + logger
            var settingsPath = PathHelpers.GetDefaultSettingsFile();

            // Bootstrap logger into default location first.
            var bootstrapLogger = new AppLogger(PathHelpers.GetDefaultLogsFolder());
            var settingsStore = new SettingsStore(settingsPath);
            var settings = settingsStore.LoadOrCreateDefault(bootstrapLogger);

            _logger = bootstrapLogger;

            // Playback
            _playback = new LibVlcPlaybackEngine(_logger);
            _previewPlayback = new LibVlcPlaybackEngine(_logger);

            // Windows
            var output = new OutputWindow();
            var control = new ControlWindow();
            var preview = new PreviewWindow();

            desktop.MainWindow = control;

            // Services that require a window
            var fileDialogs = new AvaloniaFileDialogService(control);
            var prompts = new AvaloniaUserPromptService(control);

            _folderWatch = new FolderWatchService(settings, _logger);

            _displayService = new DisplayService(control);
            var playlistStore = new PlaylistStore(PathHelpers.GetDefaultPlaylistFile());


            var vm = new ControlViewModel(
                controlWindow: control,
                outputWindow: output,
                previewWindow: preview,
                logger: _logger,
                settings: settings,
                settingsStore: settingsStore,
                playlistStore: playlistStore,
                playback: _playback,
                previewPlayback: _previewPlayback,
                folderWatch: _folderWatch,
                fileDialogs: fileDialogs,
                prompts: prompts,
                display: _displayService);

            control.DataContext = vm;

            // Show windows
            output.Show();
            if (settings.EnablePreviewWindow)
                preview.Show();
            control.Show();

            // Ensure cleanup
            desktop.Exit += (_, __) =>
            {
                try { _displayService?.Dispose(); } catch { }
                try { _folderWatch?.Dispose(); } catch { }
                try { _playback?.Dispose(); } catch { }
                try { _previewPlayback?.Dispose(); } catch { }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
