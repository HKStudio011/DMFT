using System.Collections.Concurrent;
using DMFT.Entities;

namespace DMFT.Services;

public interface IDownloadQueue
{
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

    public DownloadQueue(IDownloadEngine engine, DownloadService downloadService, IAppSettingsService settings)
    {
        _engine = engine;
        _downloadService = downloadService;

        var maxConc = settings.GetInt("maxConcurrent", 3);
        var delay = settings.GetInt("delayBetweenMs", 2000);
        _maxConcurrent = Math.Max(1, maxConc);
        _delayBetweenMs = Math.Max(500, delay);
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
