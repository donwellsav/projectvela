using System.Collections.ObjectModel;
using ConferencePlayer.Core;
using ConferencePlayer.Utils;

namespace ConferencePlayer.ViewModels;

public class HelpShortcutsViewModel : ObservableObject
{
    public ObservableCollection<ShortcutItem> Shortcuts { get; }

    public HelpShortcutsViewModel(AppSettings settings)
    {
        Shortcuts = new ObservableCollection<ShortcutItem>
        {
            new("Play / Pause", settings.HotKey_PlayPause),
            new("Stop", settings.HotKey_Stop),
            new("Play Next (Immediate)", settings.HotKey_PlayNext),
            new("Play Prev (Immediate)", settings.HotKey_PlayPrev),
            new("Frame Step", settings.HotKey_FrameStep),
            new("Select Next (Cue)", settings.HotKey_SelectNext),
            new("Select Prev (Cue)", settings.HotKey_SelectPrev),
            new("Seek Forward (+10s)", settings.HotKey_SeekForward),
            new("Seek Back (-10s)", settings.HotKey_SeekBack),
            new("Speed Up", settings.HotKey_IncreaseSpeed),
            new("Speed Down", settings.HotKey_DecreaseSpeed),
            new("Panic Blackout", settings.HotKey_Panic),
            new("Add Files", settings.HotKey_AddFiles),
            new("Add Folder", settings.HotKey_AddFolder),
            new("Play Selected", "Enter"),
            new("Remove Selected", "Delete")
        };
    }
}

public record ShortcutItem(string Action, string Key);
