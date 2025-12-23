using System;

namespace DMFT.Model
{
    public class LinkInfo
    {
        public string Url { get; set; } = string.Empty;
        public int Status { get; set; }
        // Removed progress and creation fields per request
        // public string Direction { get; set; } = string.Empty;
        public DateTime Time { get; set; } = DateTime.Now;

        // TikTok fields kept for references
        public string VideoId { get; set; } = string.Empty;
        public string OriginalUrl { get; set; } = string.Empty;
        // Removed CreatorName and VideoDurationSeconds as per request
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string TitleDescription { get; set; } = string.Empty;
        public WatermarkPreference WatermarkPreference { get; set; } = WatermarkPreference.NoPreference;
        public DownloadFormat DownloadFormat { get; set; } = DownloadFormat.Both;
        public TikTokVideoInfo TikTokMetadata { get; set; } = new TikTokVideoInfo();
        public string OriginalSoundUrl { get; set; } = string.Empty;
        public string OriginalSoundName { get; set; } = string.Empty;
        // New: storage location for this link's data
        public string SaveLocation { get; set; } = string.Empty;
        public DownloadMode DownloadMode { get; set; } = DownloadMode.Video;
    }
}
