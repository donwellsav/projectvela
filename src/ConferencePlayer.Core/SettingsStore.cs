using System;
using System.IO;
using System.Text.Json;

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
                settings.EnsureDefaults();

                if (string.IsNullOrWhiteSpace(settings.LogsFolderPath))
                {
                    settings.LogsFolderPath = PathHelpers.GetDefaultLogsFolder();
                }

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
        defaults.EnsureDefaults();
        Save(defaults, logger);
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
}
