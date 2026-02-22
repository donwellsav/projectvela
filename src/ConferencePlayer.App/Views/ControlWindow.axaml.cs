using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
        if (e.Data.Contains(DataFormats.Files))
            e.DragEffects = DragDropEffects.Copy;
        else
            e.DragEffects = DragDropEffects.None;
    }

    private void Playlist_Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is not ControlViewModel vm)
            return;

        var files = e.Data.GetFileNames();
        if (files == null)
            return;

        vm.AddFiles(files.ToList());
    }
}
