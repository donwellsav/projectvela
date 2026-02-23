using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using ConferencePlayer.Views;
using ConferencePlayer.Core;

namespace ConferencePlayer.Services;

public sealed class AvaloniaUserPromptService : IUserPromptService
{
    private readonly Window _owner;

    public AvaloniaUserPromptService(Window owner)
    {
        _owner = owner;
    }

    public async Task<UserChoice> ShowPlaybackErrorAsync(string message, string? details)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            var dialog = new PromptDialog(message, details);
            return await dialog.ShowDialog<UserChoice>(_owner);
        }

        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialog = new PromptDialog(message, details);
            return await dialog.ShowDialog<UserChoice>(_owner);
        });
    }
}
