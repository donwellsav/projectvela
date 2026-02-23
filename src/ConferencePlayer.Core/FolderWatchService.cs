using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConferencePlayer.Core;

public sealed class FolderWatchService : IDisposable
{
    private readonly AppSettings _settings;
    private readonly AppLogger _logger;
    private readonly MediaFileFilter _filter;

    private FileSystemWatcher? _watcher;
    private CancellationTokenSource? _scanCts;
    private readonly ConcurrentDictionary<string, byte> _seen = new(StringComparer.OrdinalIgnoreCase);

    // Changed to support batching
    public event EventHandler<IEnumerable<string>>? MediaFilesDetected;

    private const int MaxRecursionDepth = 20;
    private const int BatchSize = 50;

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
        // Cancel any running scan
        _scanCts?.Cancel();
        _scanCts?.Dispose();
        _scanCts = null;

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
        // Cancel previous scan if any
        _scanCts?.Cancel();
        _scanCts?.Dispose();
        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;

        return Task.Run(() => ScanRecursivelySafe(_settings.WatchedFolderPath, token));
    }

    private void ScanRecursivelySafe(string rootPath, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            return;

        try
        {
            var batch = new List<string>(BatchSize);
            ScanDirectory(rootPath, 0, batch, token);

            // Flush remaining
            if (batch.Count > 0)
            {
                RaiseDetected(batch);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Info("ScanExistingAsync cancelled.");
        }
        catch (Exception ex)
        {
            _logger.Error("ScanExistingAsync failed.", ex);
        }
    }

    private void ScanDirectory(string dir, int depth, List<string> batch, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;

        if (depth > MaxRecursionDepth)
        {
            _logger.Warn($"Max recursion depth ({MaxRecursionDepth}) reached at '{dir}'. Skipping subdirectories.");
            return;
        }

        try
        {
            // 1. Process files in current directory
            foreach (var file in Directory.EnumerateFiles(dir))
            {
                if (token.IsCancellationRequested) return;

                if (_filter.IsAllowed(file))
                {
                    batch.Add(file);
                    if (batch.Count >= BatchSize)
                    {
                        RaiseDetected(batch.ToList()); // copy
                        batch.Clear();
                    }
                }
            }

            // 2. Recurse into subdirectories if enabled
            if (_settings.IncludeSubfolders)
            {
                foreach (var subDir in Directory.EnumerateDirectories(dir))
                {
                    if (token.IsCancellationRequested) return;
                    ScanDirectory(subDir, depth + 1, batch, token);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            _logger.Warn($"Access denied to folder: '{dir}'. Skipping.");
        }
        catch (PathTooLongException)
        {
            _logger.Warn($"Path too long at: '{dir}'. Skipping.");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error scanning folder '{dir}'", ex);
        }
    }

    private void OnCreatedOrRenamed(object sender, FileSystemEventArgs e)
    {
        var path = e.FullPath;

        // De-dupe noisy watcher events.
        if (!_seen.TryAdd(path, 0))
            return;

        // Use a local CTS for individual file events, linked to the main scan CTS if needed?
        // Actually, individual events are separate tasks.
        _ = Task.Run(async () =>
        {
            try
            {
                if (!_filter.IsAllowed(path))
                    return;

                // Wait for file to be stable
                // We create a temporary CTS for this operation since StableFileAwaiter needs one
                using var fileCts = new CancellationTokenSource();

                // If the service is stopped, we should probably cancel this?
                // But _scanCts is for the scan.
                // Let's just use a reasonable timeout.

                var ok = await StableFileAwaiter.WaitForStableAsync(
                    filePath: path,
                    stableFor: TimeSpan.FromSeconds(1),
                    timeout: TimeSpan.FromSeconds(30),
                    logger: _logger,
                    ct: fileCts.Token);

                if (ok)
                {
                    RaiseDetected(new[] { path });
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"FolderWatchService background handler failed for '{path}'", ex);
            }
        });
    }

    private void RaiseDetected(IEnumerable<string> paths)
    {
        try
        {
            var list = paths.ToList();
            if (list.Count == 0) return;

            MediaFilesDetected?.Invoke(this, list);
            _logger.Info($"Detected {list.Count} media file(s). First: {list[0]}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error raising MediaFilesDetected", ex);
        }
    }

    public void Dispose()
    {
        Stop();
        _scanCts?.Cancel();
        _scanCts?.Dispose();
    }
}
