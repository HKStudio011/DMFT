using DMFT.Entities;

namespace DMFT.Services;

public interface IDownloadEngine
{
    Task StartDownloadAsync(DownloadItem item);
    Task CancelDownloadAsync(DownloadItem item);
}

public class DownloadEngine : IDownloadEngine
{
    private readonly IMediaDownloader _mediaDownloader;
    private readonly DownloadService _downloadService;
    private readonly ITikTokSoundExtractor _soundExtractor;
    private DownloadItem? _currentItem;
    private Timer? _progressTimer;
    private const int ProgressRefreshMs = 500;

    public DownloadEngine(IMediaDownloader mediaDownloader, DownloadService downloadService, ITikTokSoundExtractor soundExtractor)
    {
        _mediaDownloader = mediaDownloader;
        _downloadService = downloadService;
        _soundExtractor = soundExtractor;
        _mediaDownloader.OnProgress += HandleProgress;
    }

    private void HandleProgress(DownloadProgress progress)
    {
        if (_currentItem == null) return;
        _currentItem.DownloadedBytes = progress.DownloadedBytes;
        _currentItem.TotalBytes = progress.TotalBytes;
        _currentItem.Speed = progress.Speed;
        _currentItem.EtaSeconds = progress.EtaSeconds;
        if (progress.TotalBytes > 0)
            _currentItem.ProgressPercent = (int)((progress.DownloadedBytes * 100) / progress.TotalBytes);
    }

    public async Task StartDownloadAsync(DownloadItem item)
    {
        if (item == null) return;
        _currentItem = item;
        item.Status = StatusCodes.Downloading;
        item.DownloadedBytes = 0;
        item.TotalBytes = 0;
        item.Speed = 0;
        item.EtaSeconds = 0;
        item.ProgressPercent = 0;
        await _downloadService.UpdateDownloadAsync(item);

        var mode = (DownloadMode)item.DownloadMode;

        _progressTimer = new Timer(async _ =>
        {
            await _downloadService.UpdateDownloadAsync(item);
        }, null, ProgressRefreshMs, ProgressRefreshMs);

        try
        {
            string videoDest = Path.Combine(item.SaveLocation, $"{item.VideoId}_video.mp4");
            string audioDest = Path.Combine(item.SaveLocation, $"{item.VideoId}_audio.mp3");
            string originDest = Path.Combine(item.SaveLocation, $"{item.VideoId}_origin.mp3");

            if (mode.HasFlag(DownloadMode.OriginAudio))
            {
                var (soundName, soundUrl) = await _soundExtractor.GetOriginalSoundAsync(item.Url);
                if (!string.IsNullOrWhiteSpace(soundUrl))
                {
                    item.OriginalSoundName = soundName ?? "";
                    item.OriginalSoundUrl = soundUrl;
                    item.OriginalUrl = item.Url;
                    await _downloadService.UpdateDownloadAsync(item);
                }
            }

            var tasks = new List<Task>();
            var filenames = new List<string>();

            if (mode.HasFlag(DownloadMode.Video))
            {
                filenames.Add(Path.GetFileName(videoDest));
                tasks.Add(_mediaDownloader.DownloadAsync(item.Url, videoDest, noWatermark: true));
            }

            if (mode.HasFlag(DownloadMode.Audio))
            {
                filenames.Add(Path.GetFileName(audioDest));
                tasks.Add(_mediaDownloader.DownloadAudioAsync(item.Url, audioDest));
            }

            if (mode.HasFlag(DownloadMode.OriginAudio) && !string.IsNullOrWhiteSpace(item.OriginalSoundUrl))
            {
                filenames.Add(Path.GetFileName(originDest));
                tasks.Add(_mediaDownloader.DownloadAudioAsync(item.OriginalSoundUrl, originDest));
            }

            item.CurrentFileName = string.Join(", ", filenames);

            if (tasks.Count == 0)
                throw new Exception("No download tasks selected");

            await Task.WhenAll(tasks);

            item.Status = StatusCodes.Success;
            _progressTimer?.Dispose();
            _progressTimer = null;
            await _downloadService.MoveToHistoryAsync(item);
        }
        catch (Exception)
        {
            if (mode.HasFlag(DownloadMode.Video) && mode.HasFlag(DownloadMode.OriginAudio) && !mode.HasFlag(DownloadMode.Audio))
                item.Status = StatusCodes.VideoAudioOriginError;
            else if (mode.HasFlag(DownloadMode.Video))
                item.Status = StatusCodes.VideoError;
            else if (mode.HasFlag(DownloadMode.Audio))
                item.Status = StatusCodes.AudioOnlyError;
            else if (mode.HasFlag(DownloadMode.OriginAudio))
                item.Status = StatusCodes.AudioOriginError;
            else
                item.Status = StatusCodes.Error;
            _progressTimer?.Dispose();
            _progressTimer = null;
            await _downloadService.UpdateDownloadAsync(item);
        }
    }

    public async Task CancelDownloadAsync(DownloadItem item)
    {
        _progressTimer?.Dispose();
        _progressTimer = null;
        await _mediaDownloader.CancelAsync();
    }
}
