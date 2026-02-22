namespace ConferencePlayer.Playback;

public enum PlaybackState
{
    Idle = 0,
    Playing = 1,
    Paused = 2,
    Stopped = 3,
    Error = 4,
    PanicBlackout = 5,
}
