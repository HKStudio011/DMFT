using DMFT.Data;
using DMFT.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace DMFT.Services;

public class AppSettingsService : IAppSettingsService
{
    private Dictionary<string, string> _cache = new();
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AppSettingsService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task InitAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        _cache = await db.AppSettings.ToDictionaryAsync(s => s.Id, s => s.Value);
    }

    public string? Get(string key)
    {
        return _cache.TryGetValue(key, out var val) ? val : null;
    }

    public int GetInt(string key, int defaultValue)
    {
        if (_cache.TryGetValue(key, out var val) && int.TryParse(val, out var parsed))
            return parsed;
        return defaultValue;
    }

    public async Task SetAsync(string key, string value)
    {
        _cache[key] = value;
        using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.AppSettings.FindAsync(key);
        if (existing == null)
            db.AppSettings.Add(new AppSetting { Id = key, Value = value });
        else
            existing.Value = value;
        await db.SaveChangesAsync();
    }

    public async Task ApplyThemeAsync(IJSRuntime js)
    {
        var theme = Get("theme") ?? "system";
        var color = Get("accentColor") ?? "blue";
        await js.InvokeVoidAsync("dmftTheme.applyTheme", theme, color);
    }
}
