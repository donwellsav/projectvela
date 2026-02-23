using Avalonia.Platform;
using LibVLCSharp.Shared;

namespace ConferencePlayer.Views;

public interface IOutputWindow
{
    void AttachMediaPlayer(MediaPlayer player);
    void MoveToScreen(Screen screen);
    void Show();
}
