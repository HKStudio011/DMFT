using DMFT.Shared.Services;

namespace DMFT.Web.Client.Services;

public class FolderPicker : IFolderPicker
{
    public Task<string?> PickFolderAsync() => Task.FromResult<string?>(null);
}
