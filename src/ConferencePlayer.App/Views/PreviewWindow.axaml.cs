using Avalonia.Controls;
using LibVLCSharp.Shared;

namespace ConferencePlayer.Views;

public partial class PreviewWindow : Window
{
    public PreviewWindow()
    {
        InitializeComponent();
        SetBlackout(true);
    }

    public void AttachMediaPlayer(MediaPlayer mediaPlayer)
    {
        VideoView.MediaPlayer = mediaPlayer;
    }

    public void SetBlackout(bool enabled)
    {
        VideoView.IsVisible = !enabled;
    }
}
