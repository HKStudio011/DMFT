using DMFT.Core.Entities;

namespace DMFT.Core.Services;

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

        _progressTimer = new Timer(async _ =>
        {
            await _downloadService.UpdateDownloadAsync(item);
        }, null, ProgressRefreshMs, ProgressRefreshMs);

        try
        {
            string videoDest = Path.Combine(item.SaveLocation, $"{item.VideoId}_video.mp4");
            string audioDest = Path.Combine(item.SaveLocation, $"{item.VideoId}_audio.mp3");

            var modeFlag = (DownloadMode)item.DownloadMode;
            if (modeFlag.HasFlag(DownloadMode.OriginAudio))
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

            Task? videoTask = null;
            Task? audioTask = null;

            switch ((DownloadMode)item.DownloadMode)
            {
                case DownloadMode.Video | DownloadMode.OriginAudio:
                    item.CurrentFileName = Path.GetFileName(videoDest);
                    videoTask = _mediaDownloader.DownloadAsync(item.Url, videoDest, noWatermark: true);
                    if (!string.IsNullOrWhiteSpace(item.OriginalSoundUrl))
                    {
                        item.CurrentFileName = Path.GetFileName(audioDest);
                        audioTask = _mediaDownloader.DownloadAudioAsync(item.OriginalSoundUrl, audioDest);
                    }
                    if (videoTask != null && audioTask != null)
                        await Task.WhenAll(videoTask, audioTask);
                    else
                        throw new Exception("Missing download tasks");
                    break;

                case DownloadMode.Video:
                    item.CurrentFileName = Path.GetFileName(videoDest);
                    videoTask = _mediaDownloader.DownloadAsync(item.Url, videoDest, noWatermark: true);
                    if (videoTask != null)
                        await videoTask;
                    else
                        throw new Exception("Video download task missing");
                    break;

                case DownloadMode.OriginAudio:
                    if (!string.IsNullOrWhiteSpace(item.OriginalSoundUrl))
                    {
                        item.CurrentFileName = Path.GetFileName(audioDest);
                        audioTask = _mediaDownloader.DownloadAudioAsync(item.OriginalSoundUrl, audioDest);
                        if (audioTask != null)
                            await audioTask;
                        else
                            throw new Exception("Audio origin download failed");
                    }
                    else
                        throw new Exception("No audio URL");
                    break;

                case DownloadMode.Audio:
                    if (!string.IsNullOrWhiteSpace(item.Url))
                    {
                        item.CurrentFileName = Path.GetFileName(audioDest);
                        audioTask = _mediaDownloader.DownloadAudioAsync(item.Url, audioDest);
                        if (audioTask != null)
                            await audioTask;
                        else
                            throw new Exception("Audio only failed");
                    }
                    else
                        throw new Exception("Video URL missing for audio only");
                    break;
            }

            item.Status = StatusCodes.Success;
            _progressTimer?.Dispose();
            _progressTimer = null;
            await _downloadService.MoveToHistoryAsync(item);
        }
        catch (Exception)
        {
            item.Status = ((DownloadMode)item.DownloadMode) switch
            {
                DownloadMode.Video | DownloadMode.OriginAudio => StatusCodes.VideoAudioOriginError,
                DownloadMode.Video => StatusCodes.VideoError,
                DownloadMode.OriginAudio => StatusCodes.AudioOriginError,
                DownloadMode.Audio => StatusCodes.AudioOnlyError,
                _ => StatusCodes.Error
            };
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
