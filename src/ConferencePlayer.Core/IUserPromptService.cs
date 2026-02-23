using System.Threading.Tasks;

namespace ConferencePlayer.Core;

public interface IUserPromptService
{
    Task<UserChoice> ShowPlaybackErrorAsync(string message, string? details);
}
