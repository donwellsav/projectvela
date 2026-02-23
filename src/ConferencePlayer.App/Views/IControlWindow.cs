using System;
using ConferencePlayer.Core;
using LibVLCSharp.Shared;

namespace ConferencePlayer.Views;

public interface IControlWindow
{
    void AttachPreviewPlayer(MediaPlayer player);
    event EventHandler? Opened;
    bool Topmost { get; set; }
    void ApplyHotkeys(AppSettings settings);
}
