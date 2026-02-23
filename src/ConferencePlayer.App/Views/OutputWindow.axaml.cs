using Avalonia.Controls;
using Avalonia.Platform;
using LibVLCSharp.Shared;

namespace ConferencePlayer.Views;

public partial class OutputWindow : Window, IOutputWindow
{
    public OutputWindow()
    {
        InitializeComponent();
        SetBlackout(true);

        var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        Title = $"Project Vela - Output v{v?.ToString(3)}";
    }

    public void AttachMediaPlayer(MediaPlayer mediaPlayer)
    {
        VideoView.MediaPlayer = mediaPlayer;
    }

    public void SetBlackout(bool enabled)
    {
        // No overlays over video; we just hide the video surface.
        VideoView.IsVisible = !enabled;
    }

    public void MoveToScreen(Screen screen)
    {
        // Best-effort multi-monitor placement. This should be tested on your hardware setup.
        var bounds = screen.Bounds;

        WindowStartupLocation = WindowStartupLocation.Manual;

        Position = bounds.Position;
        Width = bounds.Width / screen.Scaling;
        Height = bounds.Height / screen.Scaling;
    }
}
