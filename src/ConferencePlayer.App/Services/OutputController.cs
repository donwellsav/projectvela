using Avalonia.Threading;
using ConferencePlayer.Core;
using ConferencePlayer.Views;

namespace ConferencePlayer.Services;

public sealed class OutputController : IOutputController
{
    private readonly OutputWindow _outputWindow;

    public OutputController(OutputWindow outputWindow)
    {
        _outputWindow = outputWindow;
    }

    public void SetBlackout(bool enabled)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            _outputWindow.SetBlackout(enabled);
        }
        else
        {
            Dispatcher.UIThread.Post(() => _outputWindow.SetBlackout(enabled));
        }
    }
}
