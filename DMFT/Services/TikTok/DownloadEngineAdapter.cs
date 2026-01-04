using System;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Maui.Storage;
using System.Net.Http;
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

        public DownloadEngineAdapter(IMediaDownloader downloader, HistoryContainer history, MainContainer main, SeleniumServices seleniumServices)
        {
            _downloader = downloader;
            _history = history;
            _main = main;
            _seleniumServices = seleniumServices;
        }

        public async Task StartDownloadAsync(LinkInfo link)
        {
            if (link == null) return;
            link.Status = StatusMessage.Downloading;
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
                string audioDest = Path.Combine(link.SaveLocation, $"{LinkInfoTag(link.VideoId)}_audio.mp3");

                Task videoTask = null;
                Task audioTask = null;

                if (link.DownloadMode == DownloadMode.VideoAndAudioOrigin)
                {
                    videoTask = _downloader.DownloadAsync(videoUrl, videoDest, noWatermark: true);
                    if (!string.IsNullOrWhiteSpace(audioUrl))
                        audioTask = _downloader.DownloadAudioAsync(audioUrl, audioDest);
                    if (videoTask != null && audioTask != null)
                        await Task.WhenAll(videoTask, audioTask);
                    else
                        throw new Exception("Missing download tasks");
                }
                else if (link.DownloadMode == DownloadMode.Video)
                {
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
            await _downloader.CancelAsync();
        }

        private string LinkInfoTag(string id) => string.IsNullOrWhiteSpace(id) ? "audio" : id;
    }
}
