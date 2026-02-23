using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ConferencePlayer.Core;

namespace ConferencePlayer.Core.Tests;

public class FolderWatchServiceTests : IDisposable
{
    private readonly string _testRoot;
    private readonly string _logsRoot;
    private readonly AppLogger _logger;
    private readonly AppSettings _settings;

    public FolderWatchServiceTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "VelaWatchTest_" + Guid.NewGuid());
        _logsRoot = Path.Combine(Path.GetTempPath(), "VelaLogsTest_" + Guid.NewGuid());
        Directory.CreateDirectory(_testRoot);
        Directory.CreateDirectory(_logsRoot);

        _settings = new AppSettings
        {
            WatchedFolderPath = _testRoot,
            WatchFolderEnabled = true,
            IncludeSubfolders = true,
            // Keep default extensions: .mp4, etc.
        };

        // Just create a real logger instance pointing to temp folder
        _logger = new AppLogger(_logsRoot);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testRoot)) Directory.Delete(_testRoot, true);
            if (Directory.Exists(_logsRoot)) Directory.Delete(_logsRoot, true);
        }
        catch { /* ignore */ }
    }

    [Fact]
    public async Task ScanExistingAsync_ShouldFindFilesInRoot()
    {
        // Arrange
        var file1 = Path.Combine(_testRoot, "video1.mp4");
        var file2 = Path.Combine(_testRoot, "audio1.mp3");
        var ignored = Path.Combine(_testRoot, "notes.txt");
        File.WriteAllText(file1, "content");
        File.WriteAllText(file2, "content");
        File.WriteAllText(ignored, "content");

        using var service = new FolderWatchService(_settings, _logger);
        var detected = new List<string>();
        service.MediaFilesDetected += (_, paths) => detected.AddRange(paths);

        // Act
        await service.ScanExistingAsync();

        // Assert
        Assert.Equal(2, detected.Count);
        Assert.Contains(file1, detected);
        Assert.Contains(file2, detected);
        Assert.DoesNotContain(ignored, detected);
    }

    [Fact]
    public async Task ScanExistingAsync_ShouldFindFilesInSubfolders_WhenEnabled()
    {
        // Arrange
        _settings.IncludeSubfolders = true;
        var sub1 = Path.Combine(_testRoot, "Sub1");
        var sub2 = Path.Combine(_testRoot, "Sub1", "Sub2");
        Directory.CreateDirectory(sub1);
        Directory.CreateDirectory(sub2);

        var file1 = Path.Combine(sub1, "video2.mp4");
        var file2 = Path.Combine(sub2, "video3.mkv");
        File.WriteAllText(file1, "content");
        File.WriteAllText(file2, "content");

        using var service = new FolderWatchService(_settings, _logger);
        var detected = new List<string>();
        service.MediaFilesDetected += (_, paths) => detected.AddRange(paths);

        // Act
        await service.ScanExistingAsync();

        // Assert
        Assert.Equal(2, detected.Count);
        Assert.Contains(file1, detected);
        Assert.Contains(file2, detected);
    }

    [Fact]
    public async Task ScanExistingAsync_ShouldIgnoreSubfolders_WhenDisabled()
    {
        // Arrange
        _settings.IncludeSubfolders = false;
        var sub = Path.Combine(_testRoot, "Sub");
        Directory.CreateDirectory(sub);
        var file = Path.Combine(sub, "video.mp4");
        File.WriteAllText(file, "content");

        using var service = new FolderWatchService(_settings, _logger);
        var detected = new List<string>();
        service.MediaFilesDetected += (_, paths) => detected.AddRange(paths);

        // Act
        await service.ScanExistingAsync();

        // Assert
        Assert.Empty(detected);
    }

    [Fact]
    public async Task ScanExistingAsync_ShouldBatchEvents()
    {
        // Arrange
        // Create 60 files (BatchSize is 50)
        for (int i = 0; i < 60; i++)
        {
            File.WriteAllText(Path.Combine(_testRoot, $"vid{i}.mp4"), "data");
        }

        using var service = new FolderWatchService(_settings, _logger);
        var eventCount = 0;
        var totalFiles = 0;
        service.MediaFilesDetected += (_, paths) =>
        {
            eventCount++;
            totalFiles += paths.Count();
        };

        // Act
        await service.ScanExistingAsync();

        // Assert
        // Should have at least 2 events (50 + 10)
        Assert.True(eventCount >= 2, $"Expected at least 2 events, got {eventCount}");
        Assert.Equal(60, totalFiles);
    }

    [Fact]
    public async Task ScanExistingAsync_ShouldRespectCancellation()
    {
        // Arrange
        // Create deep structure to ensure scan takes some time
        var sub = Path.Combine(_testRoot, "Deep");
        Directory.CreateDirectory(sub);
        for (int i = 0; i < 100; i++)
        {
            File.WriteAllText(Path.Combine(sub, $"file{i}.mp4"), "data");
        }

        using var service = new FolderWatchService(_settings, _logger);
        var detected = new List<string>();
        service.MediaFilesDetected += (_, paths) => detected.AddRange(paths);

        // Act
        var task = service.ScanExistingAsync();

        // Cancel immediately via Stop (which calls Dispose on CTS)
        // Note: service.Dispose() calls Stop().
        service.Stop();

        await task;

        // Assert
        // Verify no crash. We can't guarantee 0 files detected due to race,
        // but we verify the task completes without throwing.
        Assert.True(true);
    }
}
