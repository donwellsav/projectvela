using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ConferencePlayer.Core;

public sealed class SettingsStore
{
    private readonly string _settingsFilePath;

    public SettingsStore(string settingsFilePath)
    {
        _settingsFilePath = settingsFilePath;
    }

    public AppSettings LoadOrCreateDefault(AppLogger logger)
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                settings.Sanitize();

                return settings;
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to load settings, using defaults. File='{_settingsFilePath}'", ex);
        }

        var defaults = new AppSettings
        {
            LogsFolderPath = PathHelpers.GetDefaultLogsFolder(),
        };
        Save(defaults, logger);
        return defaults;
    }

    public async Task<AppSettings> LoadOrCreateDefaultAsync(AppLogger logger)
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                // Use FileStream with async options for true async I/O
                await using var stream = new FileStream(_settingsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream) ?? new AppSettings();
                settings.Sanitize();

                return settings;
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to load settings, using defaults. File='{_settingsFilePath}'", ex);
        }

        var defaults = new AppSettings
        {
            LogsFolderPath = PathHelpers.GetDefaultLogsFolder(),
        };
        await SaveAsync(defaults, logger);
        return defaults;
    }

    public void Save(AppSettings settings, AppLogger logger)
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to save settings. File='{_settingsFilePath}'", ex);
        }
    }

    public async Task SaveAsync(AppSettings settings, AppLogger logger)
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to save settings. File='{_settingsFilePath}'", ex);
        }
    }
}
