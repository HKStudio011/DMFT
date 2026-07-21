using DMFT.Core.Services;

namespace DMFT.Core.Services;

public class StoragePathProvider : IStoragePathProvider
{
    private readonly string _appDataPath;

    public StoragePathProvider(string appDataPath)
    {
        _appDataPath = appDataPath;
        if (!Directory.Exists(_appDataPath))
        {
            Directory.CreateDirectory(_appDataPath);
        }
    }

    public string GetAppDataPath() => _appDataPath;
    public string GetDatabasePath() => Path.Combine(_appDataPath, "dmft.db");
}
