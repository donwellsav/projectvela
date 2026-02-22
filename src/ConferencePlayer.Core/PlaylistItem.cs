namespace ConferencePlayer.Core;

public sealed record PlaylistItem(string FilePath)
{
    public string DisplayName => System.IO.Path.GetFileName(FilePath);
}
