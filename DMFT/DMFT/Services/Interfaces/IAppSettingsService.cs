using Microsoft.JSInterop;

namespace DMFT.Services;

public interface IAppSettingsService
{
    Task InitAsync();
    string? Get(string key);
    int GetInt(string key, int defaultValue);
    Task SetAsync(string key, string value);
    Task ApplyThemeAsync(IJSRuntime js);
}
