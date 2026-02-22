using System.Threading.Tasks;

namespace ConferencePlayer.Services;

public interface IUserPromptService
{
    Task<UserChoice> ShowPlaybackErrorAsync(string message, string? details);
}
