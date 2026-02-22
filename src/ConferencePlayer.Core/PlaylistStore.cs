using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ConferencePlayer.Core;

/// <summary>
/// Persists the operator playlist to a per-user JSON file.
/// </summary>
public sealed class PlaylistStore
{
    private readonly string _playlistFilePath;

    public PlaylistStore(string playlistFilePath)
    {
        _playlistFilePath = playlistFilePath;
    }

    public IReadOnlyList<PlaylistItem> Load(AppLogger logger)
    {
        try
        {
            if (!File.Exists(_playlistFilePath))
                return Array.Empty<PlaylistItem>();

            var json = File.ReadAllText(_playlistFilePath);
            var paths = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

            // Only keep existing files (offline local files only).
            var items = paths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Where(File.Exists)
                .Select(p => new PlaylistItem(p))
                .ToList();

            logger.Info($"Playlist loaded: {items.Count} items");
            return items;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to load playlist: {_playlistFilePath}", ex);
            return Array.Empty<PlaylistItem>();
        }
    }

    public void Save(IEnumerable<PlaylistItem> playlist, AppLogger logger)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_playlistFilePath)!);

            var paths = playlist
                .Select(x => x.FilePath)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var json = JsonSerializer.Serialize(paths, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_playlistFilePath, json);

            logger.Info($"Playlist saved: {paths.Count} items");
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to save playlist: {_playlistFilePath}", ex);
        }
    }

    public void Delete(AppLogger logger)
    {
        try
        {
            if (File.Exists(_playlistFilePath))
                File.Delete(_playlistFilePath);
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to delete playlist: {_playlistFilePath}", ex);
        }
    }
}
