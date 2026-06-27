using System.Collections.Concurrent;
using DMFT.Core.Data;
using DMFT.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMFT.Core.Services;

public interface IDownloadQueue
{
    Task InitializeFromDbAsync(IDbContextFactory<AppDbContext> dbFactory);
    Task EnqueueDownloadAsync(DownloadItem item);
    bool IsProcessing { get; }
    int MaxConcurrent { get; set; }
    int DelayBetweenMs { get; set; }
    event Action? OnQueueUpdated;
}

public class DownloadQueue : IDownloadQueue
{
    private readonly IDownloadEngine _engine;
    private readonly DownloadService _downloadService;
    private readonly ConcurrentQueue<DownloadItem> _queue = new();
    private int _activeCount;
    private int _maxConcurrent = 1;
    private int _delayBetweenMs = 2000;

    public bool IsProcessing => _activeCount > 0;
    public int MaxConcurrent { get => _maxConcurrent; set => _maxConcurrent = Math.Max(1, value); }
    public int DelayBetweenMs { get => _delayBetweenMs; set => _delayBetweenMs = Math.Max(500, value); }
    public event Action? OnQueueUpdated;

    public DownloadQueue(IDownloadEngine engine, DownloadService downloadService)
    {
        _engine = engine;
        _downloadService = downloadService;
    }

    public async Task InitializeFromDbAsync(IDbContextFactory<AppDbContext> dbFactory)
    {
        try
        {
            using var db = await dbFactory.CreateDbContextAsync();
            var conc = await db.AppSettings.FindAsync("maxConcurrent");
            if (conc != null && int.TryParse(conc.Value, out var c))
                MaxConcurrent = Math.Max(1, c);

            var delay = await db.AppSettings.FindAsync("delayBetweenMs");
            if (delay != null && int.TryParse(delay.Value, out var d))
                DelayBetweenMs = Math.Max(500, d);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DownloadQueue] Failed to load settings from DB: {ex.Message}");
        }
    }

    public async Task EnqueueDownloadAsync(DownloadItem item)
    {
        if (item == null) return;
        item.Status = StatusCodes.Waiting;
        _queue.Enqueue(item);
        OnQueueUpdated?.Invoke();
        if (Interlocked.Increment(ref _activeCount) <= _maxConcurrent)
        {
            _ = Task.Run(() => ProcessQueueAsync());
        }
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            while (_queue.TryDequeue(out var item))
            {
                if (item == null || item.Status == StatusCodes.New) continue;
                item.Status = StatusCodes.Downloading;
                OnQueueUpdated?.Invoke();
                await _engine.StartDownloadAsync(item);
                await Task.Delay(_delayBetweenMs);
                OnQueueUpdated?.Invoke();
            }
        }
        finally
        {
            Interlocked.Decrement(ref _activeCount);
            OnQueueUpdated?.Invoke();
        }
    }
}
