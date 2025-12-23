using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DMFT.Model;

namespace DMFT.Services
{
    public interface IDownloadQueue
    {
        Task EnqueueDownloadAsync(LinkInfo link);
        bool IsProcessing { get; }
        event System.Action OnQueueUpdated;
    }

    public class DownloadQueue : IDownloadQueue
    {
        private readonly IDownloadEngineAdapter _downloader;
        private readonly HistoryContainer _history;
        private readonly MainContainer _main;
        private readonly ConcurrentQueue<LinkInfo> _queue = new ConcurrentQueue<LinkInfo>();
        private int _isProcessing = 0; // 0 = not processing, 1 = processing
        private readonly TimeSpan _betweenDownloads = TimeSpan.FromSeconds(2);

        public bool IsProcessing => Interlocked.CompareExchange(ref _isProcessing, 0, 0) == 1;
        public event System.Action? OnQueueUpdated;

        public DownloadQueue(IDownloadEngineAdapter downloader, HistoryContainer history, MainContainer main)
        {
            _downloader = downloader;
            _history = history;
            _main = main;
        }

        public Task EnqueueDownloadAsync(LinkInfo link)
        {
            if (link == null) return Task.CompletedTask;
            // Ensure item enters queue with Waiting status
            link.Status = StatusMessage.Waiting;
            _queue.Enqueue(link);
            OnQueueUpdated?.Invoke();
            // Start worker if not already running (fire-and-forget)
            if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
            {
                _ = Task.Run(async () => await ProcessQueueAsync());
            }
            return Task.CompletedTask;
        }

        private async Task ProcessQueueAsync()
        {
            try
            {
                while (_queue.TryDequeue(out var item))
                {
                    if (item == null) continue;
                    // mark as downloading for UI binding
                    if (item.Status == StatusMessage.New) continue;
                    item.Status = StatusMessage.Downloading;
                    OnQueueUpdated?.Invoke();
                    // perform the download using existing engine (will move to history on completion)
                    await _downloader.StartDownloadAsync(item);
                    // wait a bit before starting the next download to throttle spawning
                    await Task.Delay(_betweenDownloads);
                    OnQueueUpdated?.Invoke();
                }
            }
            finally
            {
                // finished processing
                Interlocked.Exchange(ref _isProcessing, 0);
                OnQueueUpdated?.Invoke();
            }
        }
    }
}
