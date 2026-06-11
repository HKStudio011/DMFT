namespace DMFT.Shared.Services;

public interface IStoragePathProvider
{
    string GetAppDataPath();
    string GetDatabasePath();
}
