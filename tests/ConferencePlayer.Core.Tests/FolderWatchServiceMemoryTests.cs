using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ConferencePlayer.Core;

namespace ConferencePlayer.Core.Tests;

public class FolderWatchServiceMemoryTests : IDisposable
{
    private readonly string _testRoot;
    private readonly AppLogger _logger;
    private readonly AppSettings _settings;
    private readonly FolderWatchService _service;

    public FolderWatchServiceMemoryTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "VelaMemTest_" + Guid.NewGuid());
        Directory.CreateDirectory(_testRoot);

        _settings = new AppSettings
        {
            WatchedFolderPath = _testRoot,
            WatchFolderEnabled = true,
            IncludeSubfolders = true,
        };

        _logger = new AppLogger(Path.Combine(_testRoot, "Logs"));
        _service = new FolderWatchService(_settings, _logger);
    }

    public void Dispose()
    {
        _service.Dispose();
        try
        {
            if (Directory.Exists(_testRoot)) Directory.Delete(_testRoot, true);
        }
        catch { /* ignore */ }
    }

    [Fact]
    public async Task ShouldReleaseMemoryForIgnoredFiles()
    {
        // Arrange
        var file = Path.Combine(_testRoot, "ignored.tmp");
        File.WriteAllText(file, "data");

        // Invoke private method
        var method = typeof(FolderWatchService).GetMethod("OnCreatedOrRenamed", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var args = new FileSystemEventArgs(WatcherChangeTypes.Created, _testRoot, "ignored.tmp");

        // Act
        method.Invoke(_service, new object[] { _service, args });

        // Access _seen
        var seenField = typeof(FolderWatchService).GetField("_seen", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(seenField);
        var seen = (ConcurrentDictionary<string, byte>)seenField.GetValue(_service);

        // Assert - wait briefly for the task to complete (it should return immediately for ignored files)
        // With the bug, it will remain in _seen forever.
        // Without the bug, it should be removed.

        // Allow some time for Task.Run to execute
        await Task.Delay(500);

        // Check if _seen contains the key
        bool stillSeen = seen.ContainsKey(file);

        // This assertion should fail if the bug is present
        Assert.False(stillSeen, $"Path '{file}' should be removed from _seen after processing (ignored file).");
    }

    [Fact]
    public async Task ShouldReleaseMemoryForAllowedFiles()
    {
        // Arrange
        var file = Path.Combine(_testRoot, "video.mp4");
        File.WriteAllText(file, "data");

        // Invoke private method
        var method = typeof(FolderWatchService).GetMethod("OnCreatedOrRenamed", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var args = new FileSystemEventArgs(WatcherChangeTypes.Created, _testRoot, "video.mp4");

        // Act
        method.Invoke(_service, new object[] { _service, args });

        // Access _seen
        var seenField = typeof(FolderWatchService).GetField("_seen", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(seenField);
        var seen = (ConcurrentDictionary<string, byte>)seenField.GetValue(_service);

        // Assert - wait for StableFileAwaiter (1s stable + overhead)
        // We'll wait 2.5s to be safe.
        await Task.Delay(2500);

        // Check if _seen contains the key
        bool stillSeen = seen.ContainsKey(file);

        // This assertion should fail if the bug is present
        Assert.False(stillSeen, $"Path '{file}' should be removed from _seen after processing (allowed file).");
    }
}
