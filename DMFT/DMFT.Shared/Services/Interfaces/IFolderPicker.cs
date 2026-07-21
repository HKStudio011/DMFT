namespace DMFT.Shared.Services;

public interface IFolderPicker
{
    Task<string?> PickFolderAsync();
}
