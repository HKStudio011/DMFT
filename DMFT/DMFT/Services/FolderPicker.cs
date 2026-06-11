using DMFT.Shared.Services;

namespace DMFT.Services;

public class FolderPicker : IFolderPicker
{
    public Task<string?> PickFolderAsync()
    {
        return Task.FromResult<string?>(null);
    }
}
