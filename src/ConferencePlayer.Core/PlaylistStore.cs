using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ConferencePlayer.Core;

public class PlaylistState
{
    public List<string> Items { get; set; } = new();
    public int SelectedIndex { get; set; } = -1;
    public double PositionSeconds { get; set; } = 0.0;
}

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

    public PlaylistState Load(AppLogger logger)
    {
        try
        {
            if (!File.Exists(_playlistFilePath))
                return new PlaylistState();

            var json = File.ReadAllText(_playlistFilePath);
            PlaylistState state;

            try
            {
                // Try new format first
                state = JsonSerializer.Deserialize<PlaylistState>(json) ?? new PlaylistState();

                // If Items is null/empty but json wasn't empty, check if it was the old format
                if (state.Items == null || state.Items.Count == 0)
                {
                    // Fallback check: could be old list format
                    var oldList = JsonSerializer.Deserialize<List<string>>(json);
                    if (oldList != null && oldList.Count > 0)
                    {
                        state = new PlaylistState { Items = oldList };
                    }
                }
            }
            catch
            {
                // Deserialization failed, try old format explicitly
                try
                {
                    var oldList = JsonSerializer.Deserialize<List<string>>(json);
                    state = new PlaylistState { Items = oldList ?? new List<string>() };
                }
                catch
                {
                    // Both failed
                    state = new PlaylistState();
                }
            }

            // Validate files exist (offline only)
            if (state.Items != null)
            {
                state.Items = state.Items
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim())
                    .Where(File.Exists)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            else
            {
                state.Items = new List<string>();
            }

            logger.Info($"Playlist loaded: {state.Items.Count} items, SelectedIndex: {state.SelectedIndex}, Pos: {state.PositionSeconds:F2}s");
            return state;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to load playlist: {_playlistFilePath}", ex);
            return new PlaylistState();
        }
    }

    public void Save(PlaylistState state, AppLogger logger)
    {
        try
        {
            var dir = Path.GetDirectoryName(_playlistFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            // Clean list
            state.Items = state.Items
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_playlistFilePath, json);
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to save playlist (sync): {_playlistFilePath}", ex);
        }
    }

    public async Task SaveAsync(PlaylistState state, AppLogger logger)
    {
        try
        {
            var dir = Path.GetDirectoryName(_playlistFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            // Clean list
            state.Items = state.Items
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_playlistFilePath, json);
            // Logging every save might be too verbose if we save on pause/stop often.
            // logger.Info($"Playlist saved: {state.Items.Count} items");
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
