using DMFT.Shared.Services;

namespace DMFT.Services;

public class StoragePathProvider : IStoragePathProvider
{
    private readonly string _appDataPath;

    public StoragePathProvider()
    {
        _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DMFT");
        Directory.CreateDirectory(_appDataPath);
    }

    public string GetAppDataPath() => _appDataPath;
    public string GetDatabasePath() => Path.Combine(_appDataPath, "dmft.db");
}
