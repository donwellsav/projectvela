namespace ConferencePlayer.Core;

public interface IOutputController
{
    /// <summary>
    /// Forces the output window into blackout mode (hiding video surface).
    /// </summary>
    void SetBlackout(bool enabled);
}
