using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ConferencePlayer.Core.Tests;

public class SettingsStoreTests : IDisposable
{
    private readonly string _tempSettingsPath;
    private readonly string _tempLogsDir;
    private readonly AppLogger _logger;

    public SettingsStoreTests()
    {
        _tempSettingsPath = Path.Combine(Path.GetTempPath(), $"settings_{Guid.NewGuid()}.json");
        _tempLogsDir = Path.Combine(Path.GetTempPath(), $"logs_{Guid.NewGuid()}");

        // Ensure logs dir exists (AppLogger might create it, but good to be explicit for test setup)
        Directory.CreateDirectory(_tempLogsDir);
        _logger = new AppLogger(_tempLogsDir);
    }

    public void Dispose()
    {
        if (File.Exists(_tempSettingsPath))
        {
            try { File.Delete(_tempSettingsPath); } catch { }
        }

        if (Directory.Exists(_tempLogsDir))
        {
            try { Directory.Delete(_tempLogsDir, true); } catch { }
        }
    }

    [Fact]
    public void LoadOrCreateDefault_ReturnsDefaults_WhenFileMissing()
    {
        // Arrange
        var store = new SettingsStore(_tempSettingsPath);

        // Act
        var settings = store.LoadOrCreateDefault(_logger);

        // Assert
        Assert.NotNull(settings);
        // Verify a default value
        Assert.Equal("Space", settings.HotKey_PlayPause);
        // Verify file was created with defaults
        Assert.True(File.Exists(_tempSettingsPath));
    }

    [Fact]
    public void LoadOrCreateDefault_LoadsExistingSettings()
    {
        // Arrange
        var initialSettings = new AppSettings { HotKey_PlayPause = "Ctrl+P" };
        var json = JsonSerializer.Serialize(initialSettings);
        File.WriteAllText(_tempSettingsPath, json);

        var store = new SettingsStore(_tempSettingsPath);

        // Act
        var settings = store.LoadOrCreateDefault(_logger);

        // Assert
        Assert.NotNull(settings);
        Assert.Equal("Ctrl+P", settings.HotKey_PlayPause);
    }

    [Fact]
    public void LoadOrCreateDefault_HandlesCorruptFile()
    {
        // Arrange
        File.WriteAllText(_tempSettingsPath, "{ INVALID JSON ");
        var store = new SettingsStore(_tempSettingsPath);

        // Act
        var settings = store.LoadOrCreateDefault(_logger);

        // Assert
        Assert.NotNull(settings);
        // Should fallback to defaults
        Assert.Equal("Space", settings.HotKey_PlayPause);

        // Verify error was logged
        var logFiles = Directory.GetFiles(_tempLogsDir, "*.log");
        Assert.NotEmpty(logFiles);
        var logContent = File.ReadAllText(logFiles[0]);
        Assert.Contains("Failed to load settings", logContent);
    }

    [Fact]
    public async Task LoadOrCreateDefaultAsync_ReturnsDefaults_WhenFileMissing()
    {
        // Arrange
        var store = new SettingsStore(_tempSettingsPath);

        // Act
        var settings = await store.LoadOrCreateDefaultAsync(_logger);

        // Assert
        Assert.NotNull(settings);
        Assert.Equal("Space", settings.HotKey_PlayPause);
        Assert.True(File.Exists(_tempSettingsPath));
    }

    [Fact]
    public async Task LoadOrCreateDefaultAsync_LoadsExistingSettings()
    {
        // Arrange
        var initialSettings = new AppSettings { HotKey_PlayPause = "Shift+S" };
        var json = JsonSerializer.Serialize(initialSettings);
        await File.WriteAllTextAsync(_tempSettingsPath, json);

        var store = new SettingsStore(_tempSettingsPath);

        // Act
        var settings = await store.LoadOrCreateDefaultAsync(_logger);

        // Assert
        Assert.NotNull(settings);
        Assert.Equal("Shift+S", settings.HotKey_PlayPause);
    }

    [Fact]
    public void Save_CreatesFileAndDirectory()
    {
        // Arrange
        // Use a path in a nested non-existent directory to test directory creation
        var nestedPath = Path.Combine(Path.GetTempPath(), $"nested_{Guid.NewGuid()}", "settings.json");
        var store = new SettingsStore(nestedPath);
        var settings = new AppSettings { HotKey_PlayPause = "Alt+F4" };

        try
        {
            // Act
            store.Save(settings, _logger);

            // Assert
            Assert.True(File.Exists(nestedPath));
            var json = File.ReadAllText(nestedPath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json);
            Assert.Equal("Alt+F4", loaded?.HotKey_PlayPause);
        }
        finally
        {
            var dir = Path.GetDirectoryName(nestedPath);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task SaveAsync_CreatesFileAndDirectory()
    {
        // Arrange
        var nestedPath = Path.Combine(Path.GetTempPath(), $"nested_async_{Guid.NewGuid()}", "settings.json");
        var store = new SettingsStore(nestedPath);
        var settings = new AppSettings { HotKey_PlayPause = "Ctrl+Q" };

        try
        {
            // Act
            await store.SaveAsync(settings, _logger);

            // Assert
            Assert.True(File.Exists(nestedPath));
            var json = await File.ReadAllTextAsync(nestedPath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json);
            Assert.Equal("Ctrl+Q", loaded?.HotKey_PlayPause);
        }
        finally
        {
            var dir = Path.GetDirectoryName(nestedPath);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
