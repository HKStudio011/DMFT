namespace DMFT.Services;

public interface IStoragePathProvider
{
    string GetAppDataPath();
    string GetDatabasePath();
    string GetAppLocalPath();
}
