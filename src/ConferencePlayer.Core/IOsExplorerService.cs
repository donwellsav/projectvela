namespace ConferencePlayer.Core;

public interface IOsExplorerService
{
    /// <summary>
    /// Opens the specified folder in the system's file explorer.
    /// </summary>
    /// <param name="folderPath">The full path to the folder.</param>
    void OpenFolder(string folderPath);
}
