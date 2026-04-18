using System;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Maui.Storage;
using System.Net.Http;
using System.Timers;
using DMFT.Model;

namespace DMFT.Services
{
    public interface IDownloadEngineAdapter
    {
        Task StartDownloadAsync(LinkInfo link);
        Task MoveToHistoryAsync(LinkInfo link);
        Task CancelDownloadAsync(LinkInfo link);
    }

    public class DownloadEngineAdapter : IDownloadEngineAdapter
    {
        private readonly IMediaDownloader _downloader;
        private readonly HistoryContainer _history;
        private readonly MainContainer _main;
        private readonly SeleniumServices _seleniumServices;
        private LinkInfo? _currentLink;
        private System.Timers.Timer? _progressTimer;
        private const int ProgressRefreshMs = 500;

        public DownloadEngineAdapter(IMediaDownloader downloader, HistoryContainer history, MainContainer main, SeleniumServices seleniumServices)
        {
            _downloader = downloader;
            _history = history;
            _main = main;
            _seleniumServices = seleniumServices;

            _downloader.OnProgress += HandleProgress;
        }

        private void HandleProgress(DownloadProgress progress)
        {
            if (_currentLink == null) return;

            _currentLink.DownloadedBytes = progress.DownloadedBytes;
            _currentLink.TotalBytes = progress.TotalBytes;
            _currentLink.Speed = progress.Speed;
            _currentLink.EtaSeconds = progress.EtaSeconds;

            if (progress.TotalBytes > 0)
            {
                _currentLink.ProgressPercent = (int)((progress.DownloadedBytes * 100) / progress.TotalBytes);
            }
        }

        private void StartProgressTimer()
        {
            _progressTimer = new System.Timers.Timer(ProgressRefreshMs);
            _progressTimer.Elapsed += (_, _) =>
            {
                _main.RequestRefresh();
            };
            _progressTimer.Start();
        }

        private void StopProgressTimer()
        {
            _progressTimer?.Stop();
            _progressTimer?.Dispose();
            _progressTimer = null;
        }

        public async Task StartDownloadAsync(LinkInfo link)
        {
            if (link == null) return;

            _currentLink = link;
            link.Status = StatusMessage.Downloading;
            link.DownloadedBytes = 0;
            link.TotalBytes = 0;
            link.Speed = 0;
            link.EtaSeconds = 0;
            link.ProgressPercent = 0;

            StartProgressTimer();
            try
            {
                if (link.DownloadMode == DownloadMode.VideoAndAudioOrigin || link.DownloadMode == DownloadMode.AudioOriginOnly)
                {
                    var extractor = new TikTokSoundExtractor(_seleniumServices);
                    string? audioInfo = await extractor.GetOriginalSoundUrlAsync(link.Url);
                    if (!string.IsNullOrWhiteSpace(audioInfo))
                    {
                        var infor = audioInfo.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        if (infor.Length >= 2)
                        {
                            link.OriginalSoundName = infor[0];
                            link.OriginalSoundUrl = infor[1];
                            link.OriginalUrl = link.Url;
                        }
                    }
                }

                string videoUrl = link.Url;
                string audioUrl = link.OriginalSoundUrl;
                string videoDest = Path.Combine(link.SaveLocation, $"{link.VideoId}_video.mp4");

                string audioFileName = $"{LinkInfoTag(link.VideoId)}_audio";
                if (!string.IsNullOrWhiteSpace(link.OriginalSoundName))
                {
                    audioFileName = link.OriginalSoundName +"_"+ link.VideoId;
                    foreach (char c in Path.GetInvalidFileNameChars())
                    {
                        audioFileName = audioFileName.Replace(c, '_');
                    }
                }
                string audioDest = Path.Combine(link.SaveLocation, $"{audioFileName}.mp3");

                Task videoTask = null;
                Task audioTask = null;

                if (link.DownloadMode == DownloadMode.VideoAndAudioOrigin)
                {
                    link.CurrentFileName = Path.GetFileName(videoDest);
                    videoTask = _downloader.DownloadAsync(videoUrl, videoDest, noWatermark: true);
                    if (!string.IsNullOrWhiteSpace(audioUrl))
                    {
                        link.CurrentFileName = Path.GetFileName(audioDest);
                        audioTask = _downloader.DownloadAudioAsync(audioUrl, audioDest);
                    }
                    if (videoTask != null && audioTask != null)
                        await Task.WhenAll(videoTask, audioTask);
                    else
                        throw new Exception("Missing download tasks");
                }
                else if (link.DownloadMode == DownloadMode.Video)
                {
                    link.CurrentFileName = Path.GetFileName(videoDest);
                    videoTask = _downloader.DownloadAsync(videoUrl, videoDest, noWatermark: true);
                    if (videoTask != null)
                        await videoTask;
                    else
                        throw new Exception("Video download task missing");
                }
                else if (link.DownloadMode == DownloadMode.AudioOriginOnly)
                {
                    if (!string.IsNullOrWhiteSpace(audioUrl))
                    {
                        link.CurrentFileName = Path.GetFileName(audioDest);
                        audioTask = _downloader.DownloadAudioAsync(audioUrl, audioDest);
                        if (audioTask != null)
                            await audioTask;
                        else
                            throw new Exception("Audio origin download failed");
                    }
                    else
                        throw new Exception("No audio URL");
                }
                else if (link.DownloadMode == DownloadMode.AudioOnly)
                {
                    if (!string.IsNullOrWhiteSpace(videoUrl))
                    {
                        link.CurrentFileName = Path.GetFileName(audioDest);
                        audioTask = _downloader.DownloadAudioAsync(videoUrl, audioDest);
                        if (audioTask != null)
                            await audioTask;
                        else
                            throw new Exception("Audio only failed");
                    }
                    else
                        throw new Exception("Video URL missing for audio only");
                }

                link.Status = StatusMessage.Success;
                StopProgressTimer();
                await MoveToHistoryAsync(link);
            }
            catch (Exception ex)
            {
                switch (link.DownloadMode)
                {
                    case DownloadMode.VideoAndAudioOrigin:
                        link.Status = StatusMessage.VideoAudioOriginError; break;
                    case DownloadMode.Video:
                        link.Status = StatusMessage.VideoError; break;
                    case DownloadMode.AudioOriginOnly:
                        link.Status = StatusMessage.AudioOriginError; break;
                    case DownloadMode.AudioOnly:
                        link.Status = StatusMessage.AudioOnlyError; break;
                    default:
                        link.Status = StatusMessage.Error; break;
                }
                // Surface error to user via toast and then save container to reflect state
                _main.Toast?.Show($"Lỗi tải xuống: {link.VideoId ?? link.Url} ({ex.GetType().Name}) - {ex.Message}", ToastLevel.Error, _main.ToastScope);
                StopProgressTimer();
                await _main.SaveContainerAsync();
            }
        }

        public async Task MoveToHistoryAsync(LinkInfo link)
        {
            if (link == null) return;

            if (_history.IsLoading)
            {
                await _history.LoadContainerAsync();
            }
            _history.Links.Add(link);
            await _history.SaveContainer();

            _main.Links.RemoveAll(l => l == link);
            await _main.SaveContainer();

            await _history.EnforceCapacityAsync();
        }

        public async Task CancelDownloadAsync(LinkInfo link)
        {
            if (link == null) return;
            StopProgressTimer();
            await _downloader.CancelAsync();
        }

        private string LinkInfoTag(string id) => string.IsNullOrWhiteSpace(id) ? "audio" : id;
    }
}
