using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ConferencePlayer.Core;
using ConferencePlayer.Playback;
using ConferencePlayer.Services;
using LibVLCSharp.Shared;
using ConferencePlayer.ViewModels;
using ConferencePlayer.Views;

namespace ConferencePlayer;

public partial class App : Application
{
    private AppLogger? _logger;
    private FolderWatchService? _folderWatch;
    private LibVLC? _libVLC;
    private IPlaybackEngine? _playback;
    private PlaybackStateMachine? _stateMachine;
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
            _logger.Info($"App started. Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");

            // Playback
            try
            {
                LibVLCSharp.Shared.Core.Initialize();
            }
            catch (Exception ex)
            {
                _logger.Error("Core.Initialize() failed. LibVLC native libs may be missing.", ex);
            }

            _libVLC = new LibVLC(
                "--no-video-title-show",
                "--quiet"
            );

            _playback = new LibVlcPlaybackEngine(_libVLC, _logger);
            _previewPlayback = new LibVlcPlaybackEngine(_libVLC, _logger);

            // Windows
            var output = new OutputWindow();
            var control = new ControlWindow();

            desktop.MainWindow = control;

            // Services that require a window
            var fileDialogs = new AvaloniaFileDialogService(control);
            var prompts = new AvaloniaUserPromptService(control);
            var outputController = new OutputController(output);

            _folderWatch = new FolderWatchService(settings, _logger);

            _displayService = new DisplayService(control);
            var playlistStore = new PlaylistStore(PathHelpers.GetDefaultPlaylistFile());

            _stateMachine = new PlaybackStateMachine(
                engine: _playback!,
                output: outputController,
                prompts: prompts,
                logger: _logger,
                settings: settings);

            var vm = new ControlViewModel(
                controlWindow: control,
                outputWindow: output,
                logger: _logger,
                settings: settings,
                settingsStore: settingsStore,
                playlistStore: playlistStore,
                playback: _stateMachine,
                previewPlayback: _previewPlayback!,
                folderWatch: _folderWatch,
                fileDialogs: fileDialogs,
                display: _displayService,
                libVLC: _libVLC!);

            control.DataContext = vm;

            // Show windows
            output.Show();
            control.Show();

            // Ensure cleanup
            desktop.Exit += (_, __) =>
            {
                try { _displayService?.Dispose(); } catch { }
                try { _folderWatch?.Dispose(); } catch { }
                try { _stateMachine?.Dispose(); } catch { }
                try { _previewPlayback?.Dispose(); } catch { }
                try { _libVLC?.Dispose(); } catch { }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
