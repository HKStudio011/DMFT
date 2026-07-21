using DMFT.Core.Data;
using DMFT.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMFT.Core.Services;

public class DownloadService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DownloadService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // === Download Items ===

    public async Task<List<DownloadItem>> GetMainLinksAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.DownloadItems
            .Where(x => x.Status < 4)
            .OrderBy(x => x.Time)
            .ToListAsync();
    }

    public async Task<List<DownloadItem>> GetHistoryAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.DownloadItems
            .Where(x => x.Status == 4 || x.Status == 3 || x.Status >= 99)
            .OrderByDescending(x => x.Time)
            .ToListAsync();
    }

    public async Task AddDownloadAsync(DownloadItem item)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        db.DownloadItems.Add(item);
        await db.SaveChangesAsync();
    }

    public async Task AddDownloadsAsync(IEnumerable<DownloadItem> items)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        db.DownloadItems.AddRange(items);
        await db.SaveChangesAsync();
    }

    public async Task UpdateDownloadAsync(DownloadItem item)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        db.DownloadItems.Update(item);
        await db.SaveChangesAsync();
    }

    public async Task MoveToHistoryAsync(DownloadItem item)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var tracked = await db.DownloadItems.FindAsync(item.Id);
        if (tracked != null)
        {
            tracked.Status = item.Status;
            tracked.DownloadedBytes = item.DownloadedBytes;
            tracked.TotalBytes = item.TotalBytes;
            tracked.Speed = item.Speed;
            tracked.EtaSeconds = item.EtaSeconds;
            tracked.ProgressPercent = item.ProgressPercent;
            tracked.CurrentFileName = item.CurrentFileName;
            await db.SaveChangesAsync();
        }
    }

    public async Task DeleteDownloadAsync(Guid id)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var item = await db.DownloadItems.FindAsync(id);
        if (item != null)
        {
            db.DownloadItems.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    public async Task ClearDownloadsAsync(Func<DownloadItem, bool>? filter = null)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var allItems = await db.DownloadItems.ToListAsync();
        var items = filter == null ? allItems : allItems.Where(filter).ToList();
        db.DownloadItems.RemoveRange(items);
        await db.SaveChangesAsync();
    }

    // === Settings ===

    public async Task<string> GetDefaultPathAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var setting = await db.DownloadSettings.FindAsync("default");
        return setting?.DefaultPath ?? string.Empty;
    }

    public async Task SaveDefaultPathAsync(string path)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var setting = await db.DownloadSettings.FindAsync("default");
        if (setting == null)
        {
            db.DownloadSettings.Add(new DownloadSetting { DefaultPath = path });
        }
        else
        {
            setting.DefaultPath = path;
        }
        await db.SaveChangesAsync();
    }

    // === App Settings ===

    public async Task<string?> GetAppSettingAsync(string key)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var setting = await db.AppSettings.FindAsync(key);
        return setting?.Value;
    }

    public async Task SetAppSettingAsync(string key, string value)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var setting = await db.AppSettings.FindAsync(key);
        if (setting == null)
        {
            db.AppSettings.Add(new AppSetting { Id = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }
        await db.SaveChangesAsync();
    }
}
