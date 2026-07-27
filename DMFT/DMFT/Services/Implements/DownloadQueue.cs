using System.Collections.Concurrent;
using DMFT.Entities;

namespace DMFT.Services;

public interface IDownloadQueue
{
    Task EnqueueDownloadAsync(DownloadItem item);
    bool IsProcessing { get; }
    event Action? OnQueueUpdated;
}

public class DownloadQueue : IDownloadQueue
{
    private readonly IDownloadEngine _engine;
    private readonly DownloadService _downloadService;
    private readonly IAppSettingsService _settings;
    private readonly ConcurrentQueue<DownloadItem> _queue = new();
    private volatile bool _isProcessing;

    private int DelayBetweenMs => Math.Max(500, _settings.GetInt("delayBetweenMs", 2000));

    public bool IsProcessing => _isProcessing;
    public event Action? OnQueueUpdated;

    public DownloadQueue(IDownloadEngine engine, DownloadService downloadService, IAppSettingsService settings)
    {
        _engine = engine;
        _downloadService = downloadService;
        _settings = settings;
    }

    public async Task EnqueueDownloadAsync(DownloadItem item)
    {
        if (item == null) return;
        item.Status = StatusCodes.Waiting;
        await _downloadService.UpdateDownloadAsync(item);
        _queue.Enqueue(item);
        OnQueueUpdated?.Invoke();
        if (!_isProcessing)
        {
            _isProcessing = true;
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
                OnQueueUpdated?.Invoke();
                await Task.Delay(DelayBetweenMs);
            }
        }
        finally
        {
            _isProcessing = false;
            OnQueueUpdated?.Invoke();
        }
    }
}
