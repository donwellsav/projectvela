using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Platform;

namespace ConferencePlayer.Services;

public sealed class DisplayService : IDisposable
{
    private readonly Window _windowWithScreens;

    public event EventHandler? ScreensChanged;

    public DisplayService(Window windowWithScreens)
    {
        _windowWithScreens = windowWithScreens;

        // Screens is available after the window is initialized/opened.
        _windowWithScreens.Opened += (_, __) =>
        {
            _windowWithScreens.Screens!.Changed += OnScreensChanged;
        };
    }

    public IReadOnlyList<Screen> GetAllScreens()
    {
        return _windowWithScreens.Screens?.All ?? Array.Empty<Screen>();
    }

    public Screen? GetPrimary()
    {
        return _windowWithScreens.Screens?.Primary;
    }

    public void Dispose()
    {
        if (_windowWithScreens.Screens != null)
        {
            _windowWithScreens.Screens.Changed -= OnScreensChanged;
        }
    }

    private void OnScreensChanged(object? sender, EventArgs e)
    {
        ScreensChanged?.Invoke(this, EventArgs.Empty);
    }
}
