using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConferencePlayer.Services;

public interface IFileDialogService
{
    Task<IReadOnlyList<string>> PickMediaFilesAsync();
    Task<string?> PickFolderAsync();
}
