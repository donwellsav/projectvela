using ConferencePlayer.Core;
using ConferencePlayer.Utils;

namespace ConferencePlayer.ViewModels;

public class PlaylistItemViewModel : ObservableObject
{
    private readonly PlaylistItem _model;
    private string _duration = "--:--";
    private string _status = "";

    public PlaylistItemViewModel(PlaylistItem model)
    {
        _model = model;
    }

    public PlaylistItem Model => _model;

    public string Name => _model.DisplayName;
    public string FilePath => _model.FilePath;

    public string Duration
    {
        get => _duration;
        set => Set(ref _duration, value);
    }

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }
}
