namespace DMFT.Core.Services;

public interface IStoragePathProvider
{
    string GetAppDataPath();
    string GetDatabasePath();
}
