using Avalonia.Platform;
using LibVLCSharp.Shared;

namespace ConferencePlayer.Views;

public interface IOutputWindow
{
    void AttachMediaPlayer(MediaPlayer player);
    void MoveToScreen(Screen screen);
    void Show();
    void ToggleFullscreen();
    object? DataContext { get; set; }
}
