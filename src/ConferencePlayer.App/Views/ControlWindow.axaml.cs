using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ConferencePlayer.Core;
using ConferencePlayer.ViewModels;
using LibVLCSharp.Shared;

namespace ConferencePlayer.Views;

public partial class ControlWindow : Window, IControlWindow
{
    public ControlWindow()
    {
        InitializeComponent();
    }

    public void ApplyHotkeys(AppSettings settings)
    {
        if (DataContext is not ControlViewModel vm)
            return;

        KeyBindings.Clear();

        void AddKey(string gestureStr, System.Windows.Input.ICommand command)
        {
            if (string.IsNullOrWhiteSpace(gestureStr)) return;
            try
            {
                var gesture = KeyGesture.Parse(gestureStr);
                KeyBindings.Add(new KeyBinding { Gesture = gesture, Command = command });
            }
            catch
            {
                // Ignore invalid
            }
        }

        AddKey(settings.HotKey_PlayPause, vm.TogglePlayPauseCommand);
        AddKey(settings.HotKey_Stop, vm.StopCommand);

        AddKey(settings.HotKey_PlayNext, vm.PlayNextCommand);
        AddKey(settings.HotKey_PlayPrev, vm.PlayPrevCommand);

        AddKey(settings.HotKey_FrameStep, vm.FrameStepCommand);

        AddKey(settings.HotKey_SelectNext, vm.SelectNextCommand);
        AddKey(settings.HotKey_SelectPrev, vm.SelectPrevCommand);

        AddKey(settings.HotKey_SeekForward, vm.SeekForwardCommand);
        AddKey(settings.HotKey_SeekBack, vm.SeekBackCommand);

        AddKey(settings.HotKey_IncreaseSpeed, vm.IncreaseSpeedCommand);
        AddKey(settings.HotKey_DecreaseSpeed, vm.DecreaseSpeedCommand);

        AddKey(settings.HotKey_Panic, vm.PanicCommand);

        AddKey(settings.HotKey_AddFiles, vm.AddFilesCommand);
        AddKey(settings.HotKey_AddFolder, vm.AddFolderCommand);

        // Standard keys (hardcoded for now to preserve existing behavior)
        AddKey("Enter", vm.PlaySelectedCommand);
        AddKey("Delete", vm.RemoveSelectedCommand);
    }

    public void AttachPreviewPlayer(MediaPlayer player)
    {
        PreviewVideoView.MediaPlayer = player;
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
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
