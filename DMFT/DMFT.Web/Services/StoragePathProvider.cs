using DMFT.Shared.Services;

namespace DMFT.Web.Services;

public class StoragePathProvider : IStoragePathProvider
{
    private readonly string _appDataPath;

    public StoragePathProvider(IWebHostEnvironment env)
    {
        _appDataPath = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(_appDataPath);
    }

    public string GetAppDataPath() => _appDataPath;
    public string GetDatabasePath() => Path.Combine(_appDataPath, "dmft.db");
}
