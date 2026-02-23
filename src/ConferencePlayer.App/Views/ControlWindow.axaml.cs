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
        AddKey("Escape", vm.ToggleFullscreenCommand);
    }

    public void AttachPreviewPlayer(MediaPlayer player)
    {
        PreviewVideoView.MediaPlayer = player;
    }

    protected override void OnClosed(System.EventArgs e)
    {
        if (DataContext is ControlViewModel vm)
        {
            vm.Shutdown();
        }
        base.OnClosed(e);
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private const string PlaylistItemFormat = "application/vnd.projectvela.playlistitem";
    private Point _dragStartPoint;
    private bool _isPressed;
    private Control? _dragSource;

    private void OnPlaylistItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var control = sender as Control;
        if (control != null && e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            _isPressed = true;
            _dragStartPoint = e.GetPosition(control);
            _dragSource = control;
        }
    }

    private async void OnPlaylistItemPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPressed || sender != _dragSource) return;

        var control = sender as Control;
        if (control == null) return;

        var point = e.GetPosition(control);
        // Distance check (threshold)
        if (System.Math.Abs(point.X - _dragStartPoint.X) > 3 || System.Math.Abs(point.Y - _dragStartPoint.Y) > 3)
        {
            _isPressed = false; // Stop tracking "click"
            _dragSource = null;

            if (control.DataContext is PlaylistItemViewModel item)
            {
#pragma warning disable CS0618
                var data = new DataObject();
#pragma warning restore CS0618
                data.Set(PlaylistItemFormat, item);

                // Start Drag
#pragma warning disable CS0618
                await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
#pragma warning restore CS0618
            }
        }
    }

    private void OnPlaylistItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPressed = false;
        _dragSource = null;
    }

    private void Playlist_DragOver(object? sender, DragEventArgs e)
    {
#pragma warning disable CS0618
        var data = e.Data;
#pragma warning restore CS0618

        if (e.DataTransfer.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else if (data.GetDataFormats().Contains(PlaylistItemFormat))
        {
            e.DragEffects = DragDropEffects.Move;
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

#pragma warning disable CS0618
        var data = e.Data;
#pragma warning restore CS0618

        // Handle Reordering
        if (data.GetDataFormats().Contains(PlaylistItemFormat))
        {
            var sourceItem = data.Get(PlaylistItemFormat) as PlaylistItemViewModel;
            if (sourceItem != null)
            {
                // Find target item
                // e.Source is likely the element inside the ItemTemplate (e.g. TextBlock or Grid)
                var targetControl = e.Source as Control;
                var targetItem = targetControl?.DataContext as PlaylistItemViewModel;

                // If dropping on empty space, append? Or ignore?
                // If targetItem is null, we can't determine insertion point easily without visual tree walking.
                // We'll only reorder if dropped ON another item.
                if (targetItem != null && targetItem != sourceItem)
                {
                    var oldIndex = vm.Playlist.IndexOf(sourceItem);
                    var newIndex = vm.Playlist.IndexOf(targetItem);

                    if (oldIndex >= 0 && newIndex >= 0)
                    {
                        vm.Playlist.Move(oldIndex, newIndex);
                        vm.SaveState();
                    }
                }
            }
            e.Handled = true;
            return;
        }

        // Handle Files
        var files = e.DataTransfer.TryGetFiles();
        if (files != null)
        {
            var paths = files
                .Where(f => f.Path != null)
                .Select(f => f.Path.LocalPath)
                .ToList();

            if (paths.Count > 0)
                vm.AddFiles(paths);

            e.Handled = true;
        }
    }
}
