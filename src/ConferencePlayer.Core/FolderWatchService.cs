using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ConferencePlayer.Core;

public sealed class FolderWatchService : IDisposable
{
    private readonly AppSettings _settings;
    private readonly AppLogger _logger;
    private readonly MediaFileFilter _filter;

    private FileSystemWatcher? _watcher;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, byte> _seen = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<string>? MediaFileDetected;

    public FolderWatchService(AppSettings settings, AppLogger logger)
    {
        _settings = settings;
        _logger = logger;
        _filter = new MediaFileFilter(settings);
    }

    public bool IsRunning => _watcher != null;

    public void Start()
    {
        if (IsRunning)
            return;

        if (!_settings.WatchFolderEnabled)
        {
            _logger.Info("FolderWatchService.Start called but WatchFolderEnabled=false.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.WatchedFolderPath) || !Directory.Exists(_settings.WatchedFolderPath))
        {
            _logger.Warn($"Folder watch not started. Invalid folder: '{_settings.WatchedFolderPath}'");
            return;
        }

        _logger.Info($"Starting folder watch: '{_settings.WatchedFolderPath}' (subfolders={_settings.IncludeSubfolders})");

        try
        {
            _watcher = new FileSystemWatcher(_settings.WatchedFolderPath)
            {
                IncludeSubdirectories = _settings.IncludeSubfolders,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _watcher.Created += OnCreatedOrRenamed;
            _watcher.Renamed += OnCreatedOrRenamed;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to start FileSystemWatcher for '{_settings.WatchedFolderPath}'", ex);
            _watcher = null;
        }
    }

    public void Stop()
    {
        if (_watcher == null)
            return;

        _logger.Info("Stopping folder watch.");
        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnCreatedOrRenamed;
        _watcher.Renamed -= OnCreatedOrRenamed;
        _watcher.Dispose();
        _watcher = null;

        _seen.Clear();
    }

    public Task ScanExistingAsync()
    {
        // ⚡ Bolt: Offload file scanning to background thread to prevent UI blocking.
        // Previously this ran synchronously on the caller thread (UI), causing freezes with large folders.
        return Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_settings.WatchedFolderPath) || !Directory.Exists(_settings.WatchedFolderPath))
                    return;

                var option = _settings.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                foreach (var file in Directory.EnumerateFiles(_settings.WatchedFolderPath, "*.*", option))
                {
                    if (_filter.IsAllowed(file))
                        RaiseDetected(file);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("ScanExistingAsync failed.", ex);
            }
        });
    }

    private void OnCreatedOrRenamed(object sender, FileSystemEventArgs e)
    {
        var path = e.FullPath;

        // De-dupe noisy watcher events.
        if (!_seen.TryAdd(path, 0))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                if (!_filter.IsAllowed(path))
                    return;

                var ok = await StableFileAwaiter.WaitForStableAsync(
                    filePath: path,
                    stableFor: TimeSpan.FromSeconds(1),
                    timeout: TimeSpan.FromSeconds(30),
                    logger: _logger,
                    ct: _cts.Token);

                if (ok)
                    RaiseDetected(path);
            }
            catch (OperationCanceledException)
            {
                // ignore during shutdown
            }
            catch (Exception ex)
            {
                _logger.Error($"FolderWatchService background handler failed for '{path}'", ex);
            }
        });
    }

    private void RaiseDetected(string path)
    {
        try
        {
            MediaFileDetected?.Invoke(this, path);
            _logger.Info($"Detected media file: {path}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error raising MediaFileDetected for '{path}'", ex);
        }
    }

    public void Dispose()
    {
        Stop();
        _cts.Cancel();
        _cts.Dispose();
    }
}
