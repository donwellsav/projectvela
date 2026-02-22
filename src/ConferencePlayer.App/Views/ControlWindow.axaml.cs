using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using ConferencePlayer.ViewModels;
using LibVLCSharp.Shared;

namespace ConferencePlayer.Views;

public partial class ControlWindow : Window
{
    public ControlWindow()
    {
        InitializeComponent();
    }

    public void AttachPreviewPlayer(MediaPlayer player)
    {
        PreviewVideoView.MediaPlayer = player;
    }

    private void Playlist_DragOver(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void Playlist_Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is not ControlViewModel vm)
            return;

        var files = e.DataTransfer.TryGetFiles();
        if (files == null)
            return;

        var paths = files
            .Where(f => f.Path != null)
            .Select(f => f.Path.LocalPath)
            .ToList();

        if (paths.Count > 0)
            vm.AddFiles(paths);
    }
}
