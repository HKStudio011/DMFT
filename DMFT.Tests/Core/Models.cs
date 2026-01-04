namespace DMFT.Core.Model
{
    static class StatusMessage
    {
        public const int New = 0;
        public const int Waiting = 1;
        public const int Downloading = 2;
        public const int Canceled = 3;
        public const int Success = 4;
        public const int Error = 99;
        public const int VideoAudioOriginError = 100;
        public const int VideoError = 101;
        public const int AudioOriginError = 102;
        public const int AudioOnlyError = 103;
    }

    public enum DownloadMode
    {
        Video,
        AudioOnly,
        AudioOriginOnly,
        VideoAndAudioOrigin,
    }

    public enum WatermarkPreference
    {
        Watermarked,
        Original,
        NoPreference
    }

    public enum DownloadFormat
    {
        VideoMp4,
        AudioMp3,
        Both
    }

    public class TikTokVideoInfo
    {
        public string VideoId { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public string MusicTitle { get; set; } = string.Empty;
        public bool HasWatermark { get; set; }
        public List<string> QualityOptions { get; set; } = new List<string>();
    }

    public class LinkInfo
    {
        public string Url { get; set; } = string.Empty;
        public int Status { get; set; }
        public DateTime Time { get; set; } = DateTime.Now;
        public string VideoId { get; set; } = string.Empty;
        public string OriginalUrl { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string TitleDescription { get; set; } = string.Empty;
        public WatermarkPreference WatermarkPreference { get; set; } = WatermarkPreference.NoPreference;
        public DownloadFormat DownloadFormat { get; set; } = DownloadFormat.Both;
        public TikTokVideoInfo TikTokMetadata { get; set; } = new TikTokVideoInfo();
        public string OriginalSoundUrl { get; set; } = string.Empty;
        public string OriginalSoundName { get; set; } = string.Empty;
        public string SaveLocation { get; set; } = string.Empty;
        public DownloadMode DownloadMode { get; set; } = DownloadMode.Video;
    }
}
