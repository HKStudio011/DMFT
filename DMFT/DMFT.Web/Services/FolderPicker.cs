using DMFT.Shared.Services;

namespace DMFT.Web.Services;

public class FolderPicker : IFolderPicker
{
    public Task<string?> PickFolderAsync() => Task.FromResult<string?>(null);
}
