using System;
using System.Collections.Generic;
using Avalonia.Platform;

namespace ConferencePlayer.Services;

public interface IDisplayService : IDisposable
{
    event EventHandler? ScreensChanged;
    IReadOnlyList<Screen> GetAllScreens();
    Screen? GetPrimary();
}
