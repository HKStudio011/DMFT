using DMFT.Entities;

namespace DMFT.Services;

public interface IDownloadEngine
{
    Task StartDownloadAsync(DownloadItem item);
    Task CancelDownloadAsync(DownloadItem item);
    event Action<DownloadItem>? OnItemProgress;
}

public class DownloadEngine : IDownloadEngine
{
    private readonly IMediaDownloader _mediaDownloader;
    private readonly DownloadService _downloadService;
    private readonly ISoundExtractor _soundExtractor;
    private readonly ToastService _toast;
    private DownloadItem? _currentItem;
    public event Action<DownloadItem>? OnItemProgress;

    public DownloadEngine(IMediaDownloader mediaDownloader, DownloadService downloadService, ISoundExtractor soundExtractor, ToastService toast)
    {
        _mediaDownloader = mediaDownloader;
        _downloadService = downloadService;
        _soundExtractor = soundExtractor;
        _toast = toast;
        _mediaDownloader.OnProgress += HandleProgress;
    }

    private void HandleProgress(DownloadProgress progress)
    {
        if (_currentItem == null) return;
        _currentItem.Speed = progress.Speed;
        _currentItem.EtaSeconds = progress.EtaSeconds;
        if (progress.TotalBytes > 0)
            _currentItem.ProgressPercent = (int)((progress.DownloadedBytes * 100) / progress.TotalBytes);
        OnItemProgress?.Invoke(_currentItem);
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

        if (string.IsNullOrWhiteSpace(item.SaveLocation))
            item.SaveLocation = await _downloadService.GetDefaultPathAsync();

        try
        {
            string destDir = item.SaveLocation;

            if (mode.HasFlag(DownloadMode.OriginAudio))
            {
                if (!await _soundExtractor.CheckAvailableAsync())
                {
                    _toast.Show("Origin Audio: no browser available (Chrome/Edge/Firefox)", ToastLevel.Error);
                    throw new Exception("Origin Audio requires Chrome, Edge, or Firefox browser");
                }

                if (item.Platform == "YouTubeShorts")
                {
                    var soundUrl = await _soundExtractor.GetOriginalSoundYTShortAsync(item.Url);
                    if (!string.IsNullOrWhiteSpace(soundUrl))
                    {
                        item.OriginalSoundUrl = soundUrl;
                        item.OriginalUrl = item.Url;
                        await _downloadService.UpdateDownloadAsync(item);
                        _toast.Show("Found origin audio for YT Shorts", ToastLevel.Info);
                    }
                    else
                    {
                        _toast.Show("Could not extract origin audio from this YT Shorts video", ToastLevel.Warning);
                        throw new Exception("Could not extract origin audio");
                    }
                }
                else
                {
                    var (soundName, soundUrl, videoId) = await _soundExtractor.GetOriginalSoundTiktokAsync(item.Url);
                    if (!string.IsNullOrWhiteSpace(soundUrl))
                    {
                        item.OriginalSoundName = soundName ?? "";
                        item.OriginalSoundUrl = soundUrl;
                        item.OriginalUrl = item.Url;
                        item.VideoId = videoId ?? item.VideoId;
                        await _downloadService.UpdateDownloadAsync(item);
                        _toast.Show($"Found origin audio: {soundName ?? "unknown"}", ToastLevel.Info);
                    }
                    else
                    {
                        _toast.Show("Could not extract origin audio from this video", ToastLevel.Warning);
                        throw new Exception("Could not extract origin audio");
                    }
                }
            }

            var tasks = new List<Task>();

            if (mode.HasFlag(DownloadMode.Video))
                tasks.Add(_mediaDownloader.DownloadAsync(item.Url, destDir, noWatermark: true));

            if (mode.HasFlag(DownloadMode.Audio))
                tasks.Add(_mediaDownloader.DownloadAudioAsync(item.Url, destDir));

            if (mode.HasFlag(DownloadMode.OriginAudio) && !string.IsNullOrWhiteSpace(item.OriginalSoundUrl))
            {
                if (item.Platform == "YouTubeShorts")
                    tasks.Add(_mediaDownloader.DownloadAudioAsync(item.OriginalSoundUrl, destDir));
                else
                {
                    var safeName = SanitizeFilename(item.OriginalSoundName);
                    var template = $"{safeName}-{item.VideoId}.%(ext)s";
                    tasks.Add(_mediaDownloader.DownloadAudioAsync(item.OriginalSoundUrl, destDir, template));
                }
            }

            if (tasks.Count == 0)
                throw new Exception("No download tasks selected");

            await Task.WhenAll(tasks);

            item.Status = StatusCodes.Success;
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
            await _downloadService.UpdateDownloadAsync(item);
        }
    }

    public async Task CancelDownloadAsync(DownloadItem item)
    {
        await _mediaDownloader.CancelAsync();
    }

    private static string SanitizeFilename(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "unknown";
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
    }
}
