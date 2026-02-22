using System.Threading.Tasks;
using Avalonia.Controls;
using ConferencePlayer.Views;

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
        var dialog = new PromptDialog(message, details);
        return await dialog.ShowDialog<UserChoice>(_owner);
    }
}
