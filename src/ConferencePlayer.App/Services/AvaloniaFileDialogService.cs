using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace ConferencePlayer.Services;

public sealed class AvaloniaFileDialogService : IFileDialogService
{
    private readonly Window _owner;

    public AvaloniaFileDialogService(Window owner)
    {
        _owner = owner;
    }

    public async Task<IReadOnlyList<string>> PickMediaFilesAsync()
    {
        var options = new FilePickerOpenOptions
        {
            AllowMultiple = true,
            Title = "Select media files",
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Media files")
                {
                    // Best-effort. LibVLC can handle many formats; you can expand this list later.
                    Patterns = new[] { "*.mp4", "*.mov", "*.mkv", "*.avi", "*.mxf", "*.wav", "*.mp3", "*.flac", "*.m4a", "*.aac", "*.wmv", "*.ts", "*.m2ts" }
                },
                FilePickerFileTypes.All
            }
        };

        var files = await _owner.StorageProvider.OpenFilePickerAsync(options);
        return files
            .Select(f => f.Path.LocalPath)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
    }

    public async Task<string?> PickFolderAsync()
    {
        var options = new FolderPickerOpenOptions
        {
            Title = "Select folder to watch"
        };

        var folders = await _owner.StorageProvider.OpenFolderPickerAsync(options);
        return folders.FirstOrDefault()?.Path.LocalPath;
    }
}
